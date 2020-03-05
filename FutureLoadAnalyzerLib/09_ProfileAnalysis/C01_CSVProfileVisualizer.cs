using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Common;
using Common.Steps;
using Data;
using Data.Database;
using Data.DataModel.Dst;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._08_ProfileGeneration;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using OfficeOpenXml;
using Visualizer.Sankey;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    // ReSharper disable once InconsistentNaming
    public class C01_CSVProfileVisualizer : RunableForSingleSliceWithBenchmark {
        public C01_CSVProfileVisualizer([NotNull] ServiceRepository services) : base(nameof(C01_CSVProfileVisualizer),
            Stage.ProfileAnalysis,
            300,
            services,
            false)
        {
            DevelopmentStatus.Add("Visualize profiles for every tk");
            DevelopmentStatus.Add("Compare energy use from profile with total from houses");
            DevelopmentStatus.Add("Make a pdf report of everything");
            DevelopmentStatus.Add("Make visualisations of the household profiles for each trafostation");
            DevelopmentStatus.Add("Add the checks for the total energy again");
        }

        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            {
                var smartSlice = slice.CopyThisSlice();
                smartSlice.SmartGridEnabled = true;
                var path = FilenameHelpers.GetTargetDirectory(Stage.ProfileGeneration,
                    E_ApplySmartGridToGeneratedProfiles.MySequenceNumber,
                    nameof(E_ApplySmartGridToGeneratedProfiles),
                    smartSlice,
                    Services.RunningConfig);
                var di = new DirectoryInfo(path);
                if (di.Exists) {
                    // not every scenario has a smart version
                    var csvs1 = ReadCSVFiles(GenerationOrLoad.Load, di);
                    ProcessAllCharts(csvs1, smartSlice);
                    var csvs2 = ReadCSVFiles(GenerationOrLoad.Generation, di);
                    ProcessAllCharts(csvs2, smartSlice);
                }
            }
            {
                var path = FilenameHelpers.GetTargetDirectory(Stage.ProfileGeneration,
                    B_LoadProfileGenerator.MySequenceNumber,
                    nameof(B_LoadProfileGenerator),
                    slice,
                    Services.RunningConfig);
                var di = new DirectoryInfo(Path.Combine(path, "Export"));
                if (!di.Exists) {
                    throw new FlaException("Directory " + di.FullName + " does not exist");
                }

                var csvs1 = ReadCSVFiles(GenerationOrLoad.Load, di);
                ProcessAllCharts(csvs1, slice);
                var csvs2 = ReadCSVFiles(GenerationOrLoad.Generation, di);
                ProcessAllCharts(csvs2, slice);
            }
        }

        protected override void RunChartMaking([NotNull] ScenarioSliceParameters slice)
        {
        }

        private static void CheckForDuplicateIDs([NotNull] [ItemNotNull] List<CSVFile> csvs)
        {
            Dictionary<string, CSVFile> idBySource = new Dictionary<string, CSVFile>();
            foreach (var trafoKreisResult in csvs) {
                var ids = new HashSet<string>();
                foreach (var entry in trafoKreisResult.Lines) {
                    if (ids.Contains(entry.HausanschlussID)) {
                        throw new Exception("duplicate ID in " + trafoKreisResult.FileName);
                    }

                    ids.Add(entry.HausanschlussID);
                    if (idBySource.ContainsKey(entry.HausanschlussID)) {
                        throw new FlaException("One Hausanschluss, multiple trafokreise: " + trafoKreisResult.FileName + " vs " +
                                               idBySource[entry.HausanschlussID].FileName);
                    }

                    idBySource.Add(entry.HausanschlussID, trafoKreisResult);
                }
            }
        }

        private void HouseCountsPerTrafokreis([NotNull] [ItemNotNull] List<CSVFile> files, [NotNull] ScenarioSliceParameters parameters)
        {
            string suffix = files[0].GenerationOrLoad.ToString();
            var column = 0;
            var names = new List<string>();
            var countSeries = new List<BarSeriesEntry>();
            foreach (var pair in files) {
                countSeries.Add(BarSeriesEntry.MakeBarSeriesEntry("", pair.Lines.Count, column++));
                names.Add(pair.TsName);
            }

            var filename1 = MakeAndRegisterFullFilename("TrafokreisHouseCount" + suffix + ".png", parameters);
            Services.PlotMaker.MakeBarChart(filename1, "TrafokreisHouseCount", countSeries, names);
            var electricitySeries = new List<BarSeriesEntry>();
            column = 0;
            const double factor = 1_000_000;
            foreach (var pair in files) {
                electricitySeries.Add(BarSeriesEntry.MakeBarSeriesEntry("", pair.CalculateTotalEnergy() / factor, column++));
            }

            var filename2 = MakeAndRegisterFullFilename("TrafokreisHouseElectricity" + suffix + ".png", parameters);
            Services.PlotMaker.MakeBarChart(filename2, "Energie [GWh]", electricitySeries, names);
        }

        private void MakeResultExcel([NotNull] [ItemNotNull] List<CSVFile> csvs, [NotNull] ScenarioSliceParameters slice)
        {
            string suffix = csvs[0].GenerationOrLoad.ToString();
            using (var p = new ExcelPackage()) {
                var ws = p.Workbook.Worksheets.Add("MySheet");
                var tgt = FilenameHelpers.GetTargetDirectory(MyStage,
                    SequenceNumber,
                    nameof(C01_CSVProfileVisualizer),
                    slice,
                    Services.RunningConfig);

                var row = 1;
                var col = 1;
                ws.Cells[row, col++].Value = "TrafoKreis";
                ws.Cells[row, col++].Value = "ISN ID";
                ws.Cells[row, col++].Value = "CSV Energy";
                ws.Cells[row, col].Value = "DB Energy";
                row++;
                foreach (var csvFile in csvs) {
                    foreach (var csvLine in csvFile.Lines) {
                        col = 1;
                        ws.Cells[row, col++].Value = csvFile.TsName;
                        ws.Cells[row, col++].Value = csvLine.HausanschlussID;
                        var csvEnergy = csvLine.CalculateTotalEnergy();
                        ws.Cells[row, col].Value = csvEnergy;
                    }
                }


                p.SaveAs(new FileInfo(Path.Combine(tgt, "ComparisonCSVvsDB" + suffix + ".xlsx")));
            }
        }

        private void MakeTotalsSankey1([NotNull] [ItemNotNull] List<CSVFile> csvs, [NotNull] ScenarioSliceParameters parameters)
        {
            string suffix = csvs[0].GenerationOrLoad.ToString();
            var ssa = new SingleSankeyArrow("EnergyUseTotalVsRealizedInCSV" + suffix, 1000, MyStage, SequenceNumber, Name, parameters, Services);
            var dbComplexEnergy = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            var monthlyElectricityUsePerStandorts =
                dbComplexEnergy.Fetch<double>("select YearlyElectricityUseNetz from " + nameof(MonthlyElectricityUsePerStandort));
            const int fac = 1_000_000;
            var totalSum = monthlyElectricityUsePerStandorts.Sum() / fac;

            var sumAssigned = csvs.Sum(x => x.CalculateTotalEnergy()) / fac;
            var sumMissing = totalSum - sumAssigned;
            ssa.AddEntry(new SankeyEntry("Total Electricity", totalSum, 500, Orientation.Straight));
            ssa.AddEntry(new SankeyEntry("Fehlend", sumMissing * -1, 500, Orientation.Straight));
            ssa.AddEntry(new SankeyEntry("In Lastprofilen", sumAssigned * -1, 500, Orientation.Up));
            Services.PlotMaker.MakeSankeyChart(ssa);
        }

        private void ProcessAllCharts([NotNull] [ItemNotNull] List<CSVFile> csvs1, [NotNull] ScenarioSliceParameters slice)
        {
            HouseCountsPerTrafokreis(csvs1, slice);
            CheckForDuplicateIDs(csvs1);
            MakeTotalsSankey1(csvs1, slice);
            MakeResultExcel(csvs1, slice);
            WriteSumLine(csvs1, slice);
        }

        [NotNull]
        [ItemNotNull]
        private List<CSVFile> ReadCSVFiles(GenerationOrLoad generationOrLoad, [NotNull] DirectoryInfo di)
        {
            Info("Reading " + di.FullName);
            string filepattern = "*." + generationOrLoad + ".csv";
            var csvfiles = di.GetFiles(filepattern);
            if (csvfiles.Length == 0) {
                throw new FlaException("No exported files found in: " + di.FullName + " for file pattern " + filepattern);
            }

            var csvs = new List<CSVFile>();
            var activeFiles = new List<CSVFile>();
            foreach (var info in csvfiles) {
                var cs = new CSVFile(generationOrLoad, info.Name);
                csvs.Add(cs);
                cs.MyThread = ThreadProvider.Get().MakeThreadAndStart(() => { cs.Read(info); }, "CSVProfileVisualizer");
                activeFiles.Add(cs);
                WaitForJoining(activeFiles, 8);
            }

            WaitForJoining(activeFiles, 0);
            if (csvs.Count != csvfiles.Length) {
                throw new FlaException("Missing files");
            }

            Info("Read " + csvs.Count + " for " + generationOrLoad);
            return csvs;
        }

        private void WaitForJoining([NotNull] [ItemNotNull] List<CSVFile> activeFiles, int count)
        {
            while (activeFiles.Count > count) {
                if (activeFiles[0].MyThread == null) {
                    throw new FlaException("thread was null");
                }

                activeFiles[0].MyThread.Join();
                if (activeFiles[0].Ex != null) {
                    Error(activeFiles[0].Ex.Message);
                    throw activeFiles[0].Ex;
                }

                activeFiles.RemoveAt(0);
            }
        }

        private void WriteSumLine([NotNull] [ItemNotNull] List<CSVFile> csvs1, [NotNull] ScenarioSliceParameters slice)
        {
            if (slice.DstYear != 2050 && slice.SmartGridEnabled) {
                return;
            }

            string suffix = csvs1[0].GenerationOrLoad.ToString().ToLower();
            var fn = MakeAndRegisterFullFilename("SummedProfilePerTrafokreis." + suffix + ".csv", slice);
            RowCollection rc = new RowCollection("Sheet1", "Sheet1");
            using (StreamWriter sumfile = new StreamWriter(fn)) {
                Info("writing " + csvs1.Count + " sum lines for " + suffix);
                foreach (var csv in csvs1) {
                    double[] arr = new double[35040];
                    foreach (var line in csv.Lines) {
                        for (int i = 0; i < 35040; i++) {
                            arr[i] += line.Values[i];
                        }
                    }

                    Profile p = new Profile(csv.FileName ?? throw new InvalidOperationException(), arr.ToList().AsReadOnly(), EnergyOrPower.Energy);
                    RowBuilder rb = RowBuilder.Start("Name", csv.FileName);
                    rb.Add("Value", p.EnergySum());
                    rc.Add(rb);
                    sumfile.WriteLine(csv.FileName + ";" + p.GetCSVLine());
                }

                sumfile.Close();
            }

            var xlsfn = MakeAndRegisterFullFilename("SumsPerTrafokreis." + suffix + ".xlsx", slice);
            XlsxDumper.WriteToXlsx(xlsfn, rc);
            SaveToArchiveDirectory(fn, RelativeDirectory.Trafokreise, slice);
            SaveToArchiveDirectory(xlsfn, RelativeDirectory.Report, slice);
        }

        private class CSVFile {
            public CSVFile(GenerationOrLoad generationOrLoad, [CanBeNull] string fileName)
            {
                GenerationOrLoad = generationOrLoad;
                FileName = fileName;
            }

            [CanBeNull]
            public Exception Ex { get; set; }

            [CanBeNull]
            public string FileName { get; set; }

            public GenerationOrLoad GenerationOrLoad { get; set; }

            [NotNull]
            [ItemNotNull]
            public List<CSVLine> Lines { get; } = new List<CSVLine>();

            [CanBeNull]
            public Thread MyThread { get; set; }

            [CanBeNull]
            public string TsName { get; set; }

            public double CalculateTotalEnergy()
            {
                return Lines.Sum(x => x.CalculateTotalEnergy());
            }

            public void Read([NotNull] FileInfo fi)
            {
                try {
                    FileName = fi.Name;
                    using (var sr = new StreamReader(fi.FullName)) {
                        TsName = fi.Name.Replace(".csv", "");
                        while (!sr.EndOfStream) {
                            var s = sr.ReadLine();
                            if (!string.IsNullOrWhiteSpace(s)) {
                                var arr = s.Split(';');
                                var cl = new CSVLine(arr[3]);
                                for (var i = 4; i < arr.Length; i++) {
                                    if (!string.IsNullOrWhiteSpace(arr[i])) {
                                        var d = double.Parse(arr[i]);
                                        cl.Values.Add(d);
                                    }
                                }

                                Lines.Add(cl);
                            }
                        }
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Ex = ex;
                }
            }
        }

        private class CSVLine {
            public CSVLine([NotNull] string hausanschlussID) => HausanschlussID = hausanschlussID;

            [NotNull]
            public string HausanschlussID { get; }

            [NotNull]
            public List<double> Values { get; } = new List<double>();

            public double CalculateTotalEnergy() => Values.Sum();
        }
    }
}