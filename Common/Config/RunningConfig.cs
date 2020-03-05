using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Config {
    public class RunningConfig {
        [CanBeNull] [ItemNotNull] private List<ScenarioSliceParameters> _slices;

        [Obsolete("Json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public RunningConfig()
        {
        }

        private RunningConfig([NotNull] List<Options> myOptions,
                              [NotNull] List<Stage> stagesToExecute,
                              bool makeCharts,
                              int numberOfTrafokreiseToProcess,
                              [NotNull] DirectoryConfig dc,
                              [CanBeNull] List<int> limitToYears,
                              [NotNull] [ItemNotNull] List<Scenario> limitToScenarios)
        {
            MyOptions = myOptions;
            StagesToExecute = stagesToExecute;
            MakeCharts = makeCharts;
            NumberOfTrafokreiseToProcess = numberOfTrafokreiseToProcess;
            Directories = dc;
            if (limitToYears != null) {
                LimitToYears.AddRange(limitToYears);
            }

            LimitToScenarios.AddRange(limitToScenarios);
            CheckInitalisation();
        }

        public bool CheckForLpgCalcResult { get; set; } = true;

        public bool CollectFilesForArchive { get; set; } = true;

        [NotNull]
        public DirectoryConfig Directories { get; set; }

        public int LimitLoadProfileGenerationToHouses { get; set; } = -1;

        [NotNull]
        [ItemNotNull]
        public List<Scenario> LimitToScenarios { get; set; } = new List<Scenario>();

        [NotNull]
        public List<int> LimitToYears { get; set; } = new List<int>();

        public LpgPrepareMode LpgPrepareMode { get; set; } = LpgPrepareMode.PrepareWithFullLpgLoad;

        public bool MakeCharts { get; set; }
        public bool MakeExcelPerTrafokreis { get; set; }
        public bool MakeHouseSums { get; set; }
        public bool MakeStageEntries { get; set; }

        [NotNull]
        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Options> MyOptions { get; set; }

        public int NumberOfTrafokreiseToProcess { get; set; }

        public bool OnlyPrepareProfiles { get; set; }

        [CanBeNull]
        [JsonIgnore]
        [ItemNotNull]
        public ReadOnlyCollection<ScenarioSliceParameters> Slices => _slices?.AsReadOnly();

        [NotNull]
        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Stage> StagesToExecute { get; set; }

        [NotNull]
        public RunningConfig AddScenario([NotNull] Scenario scenario)
        {
            RunningConfig rc = DeepClone();
            rc.LimitToScenarios.Add(scenario);
            return rc;
        }

        public void CheckInitalisation()
        {
            Directories.CheckInitalisation();
        }

        [NotNull]
        public RunningConfig DeepClone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<RunningConfig>(json);
        }

        public void InitializeSlices([NotNull] ILogger logger)
        {
            if (MyOptions.Contains(Options.UseOnlyPresent) && MyOptions.Contains(Options.ReadFromExcel)) {
                throw new Exception("both excel and only present are set. that doesn't work.");
            }

            if (MyOptions.Contains(Options.UseOnlyPresent)) {
                var slices = new List<ScenarioSliceParameters> {Constants.PresentSlice};
                _slices = slices;
                return;
            }

            if (MyOptions.Contains(Options.ReadFromExcel)) {
                ScenarioSheetHandler ssh = new ScenarioSheetHandler(logger);
                string excelFilename = "ScenarioDefinitions.xlsx";
                if (!File.Exists(excelFilename)) {
                    excelFilename = Path.Combine(Directories.BaseUserSettingsDirectory, "ScenarioDefinitions.xlsx");
                }

                var slices = ssh.GetData(excelFilename);
                if (slices.Count == 0) {
                    throw new Exception(
                        "Could not find the scenariodefinition.xlsx. Wrote a new one or added to the current one. Please fill and try again. ");
                }

                if (MyOptions.Contains(Options.AddPresent)) {
                    slices.Add(Constants.PresentSlice);
                }

                logger.Info("Slices left after reading: " + slices.Count, Stage.Preparation, nameof(RunningConfig));
                if (LimitToScenarios.Count > 0) {
                    foreach (var scenario in LimitToScenarios) {
                        if (slices.All(x => x.DstScenario != scenario)) {
                            throw new FlaException("Not a single scenario found for the scenario name: " + scenario);
                        }
                    }

                    slices = slices.Where(x => LimitToScenarios.Contains(x.DstScenario)).ToList();
                }

                logger.Info("Slices left after filtering for dst scenario: " + string.Join(",", LimitToScenarios) + ": " + slices.Count,
                    Stage.Preparation,
                    nameof(RunningConfig));
                if (LimitToYears.Count > 0) {
                    slices = slices.Where(x => LimitToYears.Contains(x.DstYear)).ToList();
                }

                logger.Info("Slices left after filtering for dst year: " + JsonConvert.SerializeObject(LimitToYears) + ": " + slices.Count,
                    Stage.Preparation,
                    nameof(RunningConfig));
                if (slices.Count == 0) {
                    throw new FlaException("Invalid year/scenario specified, not a single slice found");
                }

                _slices = slices;
                return;
            }

            throw new Exception("No scenario slice parameter was set.");
        }

        [NotNull]
        public static RunningConfig Load([NotNull] string path)
        {
            Console.WriteLine("Reading setttings from " + path);
            string s = File.ReadAllText(path);
            RunningConfig settings = JsonConvert.DeserializeObject<RunningConfig>(s);
            return settings;
        }

        [NotNull]
        public static RunningConfig MakeDefaults()
        {
            var myOptions = new List<Options> {Options.ReadFromExcel};
            var stagesToExecute = new List<Stage> {
                Stage.ScenarioCreation, Stage.ScenarioVisualisation
            };
            string flaprofiledir = @"c:\work\Fla\CalcserverFake";
            if (Environment.MachineName == "JLCO-LAB48") {
                flaprofiledir = @"x:\ds";
            }

            DirectoryConfig dc = new DirectoryConfig("v:\\BurgdorfStatistics\\Processing",
                @"U:\SimZukunft\RawDataForMerging",
                "v:\\dropbox\\FlaSettings",
                flaprofiledir,
                @"V:\Dropbox\LPGReleases\releases9.0.0",
                @"V:\Dropbox\BurgdorfStatistics\Sam",
                @"v:\burgdorfstatistics\ResultArchive",
                @"V:\BurgdorfStatistics\Processing\unittests",
                @"c:\work\fla\HouseJobsLabsurface");
            List<Scenario> limitToScenarios = new List<Scenario> {Scenario.FromEnum(ScenarioEnum.Utopia)};
            RunningConfig cs = new RunningConfig(myOptions, stagesToExecute, true, -1, dc, null, limitToScenarios);
            if (Environment.MachineName == "JLCO-LAB48") {
                cs.MakeStageEntries = true;
            }

            return cs;
        }

        public void SaveThis([NotNull] string path)
        {
            string s = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, s);
        }

        [NotNull]
        public RunningConfig SetYear(int year)
        {
            RunningConfig rc = DeepClone();
            rc.LimitToYears.Clear();
            rc.LimitToYears.Add(year);
            return rc;
        }
    }

    public enum LpgPrepareMode {
        PrepareWithFullLpgLoad,
        PrepareWithOnlyNamecheck
    }
}