// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Common;
using Common.Config;
using Common.Database;
using Common.Logging;
using Common.Steps;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._04_HouseMaker;
using FutureLoadAnalyzerLib._05_ScenarioCreation;
using FutureLoadAnalyzerLib._08_ProfileGeneration;
using FutureLoadAnalyzerLib._09_ProfileAnalysis;
using FutureLoadAnalyzerLib._10_CrossSliceScenarioAnalysis;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using Visualizer;
using Visualizer.Mapper;
using Visualizer.OSM;
using Xunit;
using Xunit.Abstractions;
using IContainer = Autofac.IContainer;
using Logger = Common.Logging.Logger;

namespace FutureLoadAnalyzerLib {
    public class MainBurgdorfStatisticsCreator {
        public MainBurgdorfStatisticsCreator([CanBeNull] ITestOutputHelper output)
        {
            _output = output;
            CompositeResolver.RegisterAndSetAsDefault(NativeDateTimeResolver.Instance, StandardResolver.Instance);
        }

        [CanBeNull] private readonly ITestOutputHelper _output;

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void RunBasedOnSettings([NotNull] RunningConfig settings, [NotNull] Logger logger)
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var container = CreateBuilderContainer(_output, logger, settings);
            Dictionary<string, double> secondsPerStep = new Dictionary<string, double>();
            using (var scope = container.BeginLifetimeScope()) {
                var allItems = container.Resolve<IEnumerable<IBasicRunner>>().ToList();
                logger.Info("#The following scenarios are going to be processed.", Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                if (settings.Slices == null) {
                    throw new FlaException("slices was not initalized");
                }

                foreach (ScenarioSliceParameters scenarioSliceParameters in settings.Slices) {
                    logger.Info("\t" + scenarioSliceParameters, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                }

                logger.Info("#The following stages are going to be processed.", Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                foreach (Stage stage in settings.StagesToExecute) {
                    logger.Info("\t" + stage, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                }

                for (int stageIdx = 0; stageIdx < settings.StagesToExecute.Count; stageIdx++) {
                    Stage currentStage = settings.StagesToExecute[stageIdx];
                    logger.Info("## Starting Stage " + currentStage, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                    var steps = allItems.Where(x => x.MyStage == currentStage).ToList();
                    steps.Sort((x, y) => x.SequenceNumber.CompareTo(y.SequenceNumber));
                    logger.Info("### The following steps in this stage are going to be processed.",
                        Stage.Preparation,
                        nameof(MainBurgdorfStatisticsCreator));
                    foreach (var step in steps) {
                        logger.Info("\t" + step.SequenceNumber + " - " + step.Name, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                    }

                    CheckAndValidateSteps(steps, logger, currentStage, out var stepType);

                    switch (stepType) {
                        case Steptype.Global: {
                            ProcessGlobalSteps(steps, secondsPerStep);
                            logger.SaveToDatabase(Constants.PresentSlice);
                            break;
                        }

                        case Steptype.SliceProcessors: {
                            ProcessSliceSteps(steps, logger, settings, secondsPerStep, currentStage);
                            break;
                        }

                        case Steptype.Scenario: {
                            List<RunnableForScenarioWithBenchmark> scenarioSteps = steps.Select(x => (RunnableForScenarioWithBenchmark)x).ToList();
                            var scenariosToProcess = settings.Slices.Select(x => x.DstScenario).Distinct().ToList();

                            if (scenariosToProcess.Contains(Scenario.Present()) && currentStage == Stage.ScenarioCreation) {
                                scenariosToProcess.Remove(Scenario.Present());
                            }

                            foreach (var scenario in scenariosToProcess) {
                                logger.Info("Processing " + scenario, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                                var slicesWithScenario = settings.Slices.Where(x => x.DstScenario == scenario).ToList();
                                foreach (var step in scenarioSteps) {
                                    Stopwatch stopwatch = Stopwatch.StartNew();
                                    step.Run(settings.Slices.ToList());
                                    logger.SaveToDatabase(slicesWithScenario.Last());
                                    var stepname = currentStage + "-" + step.Name;
                                    stopwatch.Stop();
                                    if (!secondsPerStep.ContainsKey(stepname)) {
                                        secondsPerStep.Add(stepname, 0);
                                    }

                                    secondsPerStep[stepname] += stopwatch.ElapsedMilliseconds / 1000.0;
                                }

                                logger.Info("Finished processing " + scenario, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                            }
                        }

                            break;
                        case Steptype.AllScenarios: {
                            List<RunnableForAllScenarioWithBenchmark> crossScenarioSteps =
                                steps.Select(x => (RunnableForAllScenarioWithBenchmark)x).ToList();
                            AnalysisRepository analysisRepository = new AnalysisRepository(settings);
                            foreach (var step in crossScenarioSteps) {
                                Stopwatch stopwatch = Stopwatch.StartNew();
                                step.Run(settings.Slices.ToList(), analysisRepository);
                                logger.SaveToDatabase(Constants.PresentSlice);
                                var stepname = currentStage + "-" + step.Name;
                                stopwatch.Stop();
                                if (!secondsPerStep.ContainsKey(stepname)) {
                                    secondsPerStep.Add(stepname, 0);
                                }

                                secondsPerStep[stepname] += stopwatch.ElapsedMilliseconds / 1000.0;
                            }
                        }

                            break;
                        default:
                            throw new Exception("unknown steptype");
                    }

                    logger.Info("Finished Stage " + currentStage + ", waiting for the charts to finish.",
                        Stage.Preparation,
                        nameof(MainBurgdorfStatisticsCreator));
                    var plotMaker = scope.Resolve<PlotMaker>();
                    plotMaker.Finish();
                    logger.Info("Finished Stage " + currentStage, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                }

                logger.SaveToDatabase(Constants.PresentSlice);
                if (settings.MakeStageEntries) {
                    PngSvgConverter psc = new PngSvgConverter(logger, Stage.Preparation);
                    psc.ConvertAllSVG(settings);
                }

                sw.Stop();
                logger.Info("Finished everything after " + sw.Elapsed.TotalMinutes + " minutes",
                    Stage.Preparation,
                    nameof(MainBurgdorfStatisticsCreator));
                foreach (var pair in secondsPerStep) {
                    logger.Info(pair.Key + "\t" + pair.Value.ToString("F2"), Stage.Preparation, "Main");
                }

                logger.Info("Listing open threads before closing the logger", Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                ThreadProvider.Get().DisplayRunningThreads();
                logger.Info("Listing open threads after closing the logger", Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                logger.FinishSavingEverything();
                ThreadProvider.Get().DisplayRunningThreads();
            }

            container.Dispose();
        }

        private static void CheckAndValidateSteps([NotNull] [ItemNotNull] List<IBasicRunner> steps,
                                                  [NotNull] Logger logger,
                                                  Stage currentStage,
                                                  out Steptype stepType)
        {
            if (steps.Count == 0) {
                throw new Exception("Not a single step in the stage " + currentStage);
            }

            stepType = Steptype.Global;
            var listOfSequenceNumbers = steps.Select(x => x.SequenceNumber).Distinct().ToList();
            var listOfStepNames = steps.Select(x => x.Name).Distinct().ToList();
            if (listOfSequenceNumbers.Count != steps.Count) {
                Dictionary<int, string> sequences = new Dictionary<int, string>();
                foreach (IBasicRunner runner in steps) {
                    if (sequences.ContainsKey(runner.SequenceNumber)) {
                        throw new Exception("Duplicate squence numbers:" + runner.SequenceNumber + " Steps: \n" + sequences[runner.SequenceNumber] +
                                            "\n" + runner.Name);
                    }

                    sequences.Add(runner.SequenceNumber, runner.Name);
                }
            }

            if (listOfStepNames.Count != steps.Count) {
                Dictionary<string, string> sequences = new Dictionary<string, string>();
                foreach (IBasicRunner runner in steps) {
                    if (sequences.ContainsKey(runner.Name)) {
                        throw new Exception("Duplicate names in sequence:" + runner.Name);
                    }

                    sequences.Add(runner.Name, runner.Name);
                }
            }

            var thisstepType = steps[0].StepType;
            stepType = thisstepType;
            if (steps.Any(x => x.StepType != thisstepType)) {
                foreach (var basicRunner in steps) {
                    logger.Info(basicRunner.Name + "- " + basicRunner.SequenceNumber + " - " + basicRunner.StepType,
                        Stage.Preparation,
                        nameof(MainBurgdorfStatisticsCreator));
                }

                throw new Exception("Inconsistent step types in stage " + currentStage);
            }

            var stepsByNumber = steps.OrderBy(x => x.SequenceNumber).ToList();
            var stepsByName = steps.OrderBy(x => x.Name).ToList();
            for (int i = 0; i < stepsByName.Count; i++) {
                if (stepsByName[i] != stepsByNumber[i]) {
                    throw new FlaException("Invalid ordering: Sequence number says it should be " + stepsByNumber[i].Name +
                                           " but name says it should be " + stepsByName[i].Name);
                }
            }
        }

        private static void ProcessSliceSteps([ItemNotNull] [NotNull] List<IBasicRunner> steps,
                                              [NotNull] Logger logger,
                                              [NotNull] RunningConfig settings,
                                              [NotNull] Dictionary<string, double> secondsPerStep,
                                              Stage currentStage)
        {
            var sliceSteps = steps.Select(x => (RunableForSingleSliceWithBenchmark)x).ToList();
            if (settings.Slices == null) {
                throw new FlaException("slices was not initalized");
            }

            List<int> years = settings.Slices.Select(x => x.DstYear).Distinct().ToList();
            years.Sort((x, y) => x.CompareTo(y));
            foreach (var year in years) {
                logger.Info("Processing " + year, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                var slicesForYear = settings.Slices.Where(x => x.DstYear == year).ToList();
                foreach (var step in sliceSteps) {
                    foreach (ScenarioSliceParameters sliceParameters in slicesForYear) {
                        if (sliceParameters.Equals(Constants.PresentSlice) && currentStage == Stage.ScenarioCreation) {
                            continue;
                        }

                        Stopwatch sw = Stopwatch.StartNew();
                        step.RunForScenarios(sliceParameters);
                        logger.SaveToDatabase(sliceParameters);
                        var stepname = step.Name;
                        sw.Stop();
                        if (!secondsPerStep.ContainsKey(stepname)) {
                            secondsPerStep.Add(stepname, 0);
                        }

                        secondsPerStep[stepname] += sw.ElapsedMilliseconds / 1000.0;
                    }
                }

                logger.Info("Finished Processing " + year, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
            }
        }

        private static void ProcessGlobalSteps([ItemNotNull] [NotNull] List<IBasicRunner> steps, [NotNull] Dictionary<string, double> secondsPerStep)
        {
            var noParameterSteps = steps.Select(x => (RunableWithBenchmark)x).ToList();
            for (int j = 0; j < noParameterSteps.Count; j++) {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                noParameterSteps[j].Run();
                var stepname = noParameterSteps[j].Name;
                sw.Stop();
                if (!secondsPerStep.ContainsKey(stepname)) {
                    secondsPerStep.Add(stepname, 0);
                }

                secondsPerStep[stepname] += sw.ElapsedMilliseconds / 1000.0;
            }
        }

        [NotNull]
        public static IContainer CreateBuilderContainer([CanBeNull] ITestOutputHelper unittestoutput,
                                                        [NotNull] Logger logger,
                                                        [NotNull] RunningConfig rc)
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.Register(_ => rc).As<RunningConfig>().SingleInstance();
            builder.Register(_ => logger).As<Logger>().As<ILogger>().SingleInstance();
            builder.Register(_ => unittestoutput).As<ITestOutputHelper>().SingleInstance();
            Random rnd = new Random(1);
            builder.Register(x => rnd).As<Random>().SingleInstance();
            builder.Register(x => new ServiceRepository(x.Resolve<PlotMaker>(),
                x.Resolve<MapDrawer>(),
                x.Resolve<Logger>(),
                x.Resolve<RunningConfig>(),
                x.Resolve<Random>())).As<ServiceRepository>().As<IServiceRepository>().SingleInstance();

            // import
            builder.RegisterType<OsmMapDrawer>().As<OsmMapDrawer>().SingleInstance();
            builder.RegisterType<PlotMaker>().As<PlotMaker>().SingleInstance();
            builder.Register(x => new MapDrawer(x.Resolve<ILogger>(), Stage.Plotting)).As<MapDrawer>().SingleInstance();
            var mainAssembly = Assembly.GetAssembly(typeof(MainBurgdorfStatisticsCreator));

            builder.RegisterAssemblyTypes(mainAssembly).Where(t => t.GetInterfaces().Contains(typeof(IBasicRunner))).AsSelf().As<IBasicRunner>();
            var container = builder.Build();
            return container;
        }

        [Fact]
        public void CopyCurrentVersionToStatistics()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            FileInfo fi = new FileInfo(Assembly.GetExecutingAssembly().Location);

            string dstPath = Path.Combine(rc.Directories.BaseProcessingDirectory, "exe", fi.Name);
            fi.CopyTo(dstPath, true);
            DirectoryInfo di = fi.Directory;
            if (di != null) {
                FileInfo[] fis = di.GetFiles("*.dll");
                foreach (FileInfo info in fis) {
                    string dstFile = Path.Combine(rc.Directories.BaseProcessingDirectory, "exe", info.Name);
                    info.CopyTo(dstFile, true);
                }
            }
        }

        [Fact]
        public void RebuildForHa()
        {
            _output?.WriteLine(DateTime.Now.ToShortTimeString());
            RebuildHouses();
            RunProfileGenerationPresent();
        }

        [Fact]
        public void RebuildHouses()
        {
            List<Stage> ptr = new List<Stage> {
                Stage.Raw,
                Stage.Complexes,
                Stage.ComplexEnergyData,
                Stage.Houses
            };
            var myOptions = new List<Options> {Options.ReadFromExcel, Options.AddPresent};
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Clear();
            settings.MyOptions.AddRange(myOptions);
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.AddRange(ptr);
            settings.MakeCharts = true;
            settings.CollectFilesForArchive = false;
            settings.NumberOfTrafokreiseToProcess = -1;
            settings.LimitLoadProfileGenerationToHouses = 50;
            settings.LimitToScenarios.Add(Scenario.Present());
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                RunBasedOnSettings(settings, logger);
            }
        }

        [Fact]
        public void RebuildScenarios()
        {
            List<Stage> ptr = new List<Stage> {
                Stage.ScenarioCreation,
                Stage.ScenarioVisualisation
            };
            var myOptions = new List<Options> {Options.ReadFromExcel, Options.AddPresent};
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Clear();
            settings.MyOptions.AddRange(myOptions);
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.AddRange(ptr);
            settings.MakeCharts = false;
            settings.NumberOfTrafokreiseToProcess = -1;
            settings.LimitToScenarios.Clear();
            settings.LimitToScenarios.Add(Scenario.FromString("Utopia"));
            settings.LimitToScenarios.Add(Scenario.FromString("Dystopia"));
            settings.LimitToScenarios.Add(Scenario.FromString("Nep"));
            settings.LimitToScenarios.Add(Scenario.FromString("Pom"));
            settings.LimitToScenarios.Add(Scenario.Present());
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                RunBasedOnSettings(settings, logger);
            }
        }

        [Fact]
        public void RunIsnImport()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            using (Logger logger = new Logger(_output, rc)) {
                using (var container = CreateBuilderContainer(_output, logger, rc)) {
                    using (var scope = container.BeginLifetimeScope()) {
                        var s1 = scope.Resolve<A02_DirenIsnImport>();
                        s1.Run();
                        var s2 = scope.Resolve<C07_SupplementalISN>();
                        s2.Run();
                        var pm = scope.Resolve<PlotMaker>();
                        pm.Finish();
                        PngSvgConverter con = new PngSvgConverter(logger, Stage.Preparation);
                        con.ConvertAllSVG(rc);
                        logger.SaveToDatabase(Constants.PresentSlice);
                        Process.Start(s2.GetThisTargetDirectory());
                    }
                }
            }
        }

        [Fact]
        public void RunProfileGenerationPresent()
        {
            List<Stage> ptr = new List<Stage> {
                Stage.ProfileAnalysis
            };
            var myOptions = new List<Options> {Options.ReadFromExcel, Options.AddPresent};
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Clear();
            settings.MyOptions.AddRange(myOptions);
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.AddRange(ptr);
            settings.MakeCharts = true;
            settings.NumberOfTrafokreiseToProcess = -1;
            settings.LimitToScenarios.Add(Scenario.Present());
            settings.LimitToYears.Add(2017);
            settings.MakeHouseSums = true;
            settings.CheckForLpgCalcResult = false;
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                RunBasedOnSettings(settings, logger);
            }
        }

        [Fact]
        public void RunProfileGenerationUtopia()
        {
            List<Stage> ptr = new List<Stage> {
                //Stage.ProfileGeneration,
                Stage.ProfileAnalysis
            };
            var myOptions = new List<Options> {Options.ReadFromExcel, Options.AddPresent};
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Clear();
            settings.MyOptions.AddRange(myOptions);
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.AddRange(ptr);
            settings.MakeCharts = true;
            settings.NumberOfTrafokreiseToProcess = -1;
            settings.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            settings.MakeHouseSums = true;
            settings.LimitToYears.Add(2050);
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                RunBasedOnSettings(settings, logger);
            }
        }

        [Fact]
        [SuppressMessage("ReSharper", "UnusedVariable")]
        public void RunSingleCrossScenarioStep()
        {
            RunningConfig settings = RunningConfig.MakeDefaults();
            List<Stage> ptr = new List<Stage> {
                Stage.ScenarioCreation
//                Stage.ScenarioVisualisation,
            };
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.AddRange(ptr);
            settings.MyOptions.Add(Options.ReadFromExcel);
            settings.MyOptions.Add(Options.AddPresent);
            settings.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            settings.LimitToYears.Add(2017);
            settings.LimitToYears.Add(2050);

            settings.LimitToScenarios.Add(Scenario.Present());
            AnalysisRepository repo = new AnalysisRepository(settings);
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                using (var container = CreateBuilderContainer(_output, logger, settings)) {
                    using (var scope = container.BeginLifetimeScope()) {
                        List<RunableForSingleSliceWithBenchmark> stuffToRun = new List<RunableForSingleSliceWithBenchmark>();
                        var a1 = scope.Resolve<A02_CrossSliceProfileAnalysis>();
                        if (settings.Slices == null) {
                            throw new FlaException("slices was not initalized");
                        }

                        a1.Run(settings.Slices.ToList(), repo);
                        var pm = scope.Resolve<PlotMaker>();
                        pm.Finish();
                        PngSvgConverter con = new PngSvgConverter(logger, Stage.Preparation);
                        con.ConvertAllSVG(settings);
                    }
                }
            }
        }

        [Fact]
        [SuppressMessage("ReSharper", "UnusedVariable")]
        public void RunSingleParameterSlice()
        {
            _output?.WriteLine("This is output from {0}", "RunSingleParameterSlice");
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Add(Options.ReadFromExcel);
            settings.LimitToYears.Add(2050);
            settings.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            settings.MakeCharts = false;
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                using (var container = CreateBuilderContainer(_output, logger, settings)) {
                    using (var scope = container.BeginLifetimeScope()) {
                        if (settings.Slices == null) {
                            throw new FlaException("slices was not initalized");
                        }

                        ScenarioSliceParameters ssp = settings.Slices[0];
                        //ssp = Constants.PresentSlice;
                        var s1 = scope.Resolve<Z_GeneralExporter>();
                        s1.RunForScenarios(ssp);
                        var pm = scope.Resolve<PlotMaker>();
                        pm.Finish();
                        PngSvgConverter con = new PngSvgConverter(logger, Stage.Preparation);
                        con.ConvertAllSVG(settings);
                        logger.SaveToDatabase(Constants.PresentSlice);
                    }
                }
            }
        }

        [Fact]
        [SuppressMessage("ReSharper", "UnusedVariable")]
        public void RunSingleParameterSliceForAllScenarios()
        {
            RunningConfig settings = RunningConfig.MakeDefaults();
            List<Stage> ptr = new List<Stage> {
                Stage.ScenarioCreation
//                Stage.ScenarioVisualisation,
            };
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.AddRange(ptr);
            settings.MyOptions.Add(Options.ReadFromExcel);
            settings.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            Logger logger = new Logger(_output, settings);
            settings.InitializeSlices(logger);

            using (var container = CreateBuilderContainer(_output, logger, settings)) {
                using (var scope = container.BeginLifetimeScope()) {
                    List<RunableForSingleSliceWithBenchmark> stuffToRun = new List<RunableForSingleSliceWithBenchmark> {scope.Resolve<A01_Houses>()};
                    if (settings.Slices == null) {
                        throw new FlaException("slices was not initalized");
                    }

                    foreach (var slice in settings.Slices) {
                        logger.Info("#######################################", Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                        logger.Info(slice.DstYear + " - " + slice.DstScenario, Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                        logger.Info("#######################################", Stage.Preparation, nameof(MainBurgdorfStatisticsCreator));
                        foreach (var runable in stuffToRun) {
                            runable.RunForScenarios(slice);
                        }

                        logger.SaveToDatabase(slice);
                    }

                    var pm = scope.Resolve<PlotMaker>();
                    pm.Finish();
                    PngSvgConverter con = new PngSvgConverter(logger, Stage.Preparation);
                    con.ConvertAllSVG(settings);
                }
            }
        }

        [Fact]
        public void RunSingleRunableStep()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            using (Logger logger = new Logger(_output, rc)) {
                using (var container = CreateBuilderContainer(_output, logger, rc)) {
                    using (var scope = container.BeginLifetimeScope()) {
                        var s1 = scope.Resolve<E01_ScenarioDefinitionSheetFixer>();
                        s1.Run();
                        var pm = scope.Resolve<PlotMaker>();
                        pm.Finish();
                        PngSvgConverter con = new PngSvgConverter(logger, Stage.Preparation);
                        con.ConvertAllSVG(rc);
                        logger.SaveToDatabase(Constants.PresentSlice);
                        Process.Start(s1.GetThisTargetDirectory());
                    }
                }
            }
        }

        [Fact]
        public void WriteListOfStages()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            List<string> stages = new List<string>();
            foreach (string name in Enum.GetNames(typeof(Stage))) {
                stages.Add(name);
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(rc.Directories.BaseProcessingDirectory, "Stages.Json"))) {
                sw.WriteLine(JsonConvert.SerializeObject(stages, Formatting.Indented));
                sw.Close();
            }
        }
    }
}