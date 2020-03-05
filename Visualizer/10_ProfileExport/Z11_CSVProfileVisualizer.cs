using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using OfficeOpenXml;
using Visualizer.Sankey;

namespace BurgdorfStatistics._10_ProfileExport {
    // ReSharper disable once InconsistentNaming
    public class Z11_CSVProfileVisualizer : RunableForSingleSliceWithBenchmark {
        private class CSVLine {
            public CSVLine([NotNull] string hausanschlussID) => HausanschlussID = hausanschlussID;

            [NotNull]
            public string HausanschlussID { get; set; }
            [NotNull]
            public List<double> Values { get; } = new List<double>();

            public double CalculateTotalEnergy() => Values.Sum();
        }

        private class CSVFile {
            [CanBeNull]
            public Thread MyThread { get; set; }
            [CanBeNull]
            public string TsName { get; set; }

            public double CalculateTotalEnergy()
            {
                return Lines.Sum(x => x.CalculateTotalEnergy());
            }

            [NotNull]
            [ItemNotNull]
            public List<CSVLine> Lines { get; } = new List<CSVLine>();

            public void Read([NotNull] FileInfo fi)
            {
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
        }

        private void MakeResultExcel([NotNull] [ItemNotNull] List<CSVFile> csvs, [NotNull] ScenarioSliceParameters slice, [NotNull] string suffix)
        {
            using (var p = new ExcelPackage()) {
                var ws = p.Workbook.Worksheets.Add("MySheet");
                var tgt = FilenameHelpers.GetTargetDirectory(Stage.ProfileGeneration, SequenceNumber, nameof(Z12_CSVProfileCopier), slice);
                //var houseEntriesByIsn = new Dictionary<int, TkEnergyEntry>();
                /* foreach (var tkr in trafoKreisResults) {
                     foreach (var tkEntry in tkr.TkEnergyEntries) {
                         if (!string.IsNullOrWhiteSpace(tkEntry.IsnIDs)) {
                             var isnIds = JsonConvert.DeserializeObject<List<int>>(tkEntry.IsnIDs);
                             if (isnIds.Count > 0) {
                                 houseEntriesByIsn.Add(isnIds[0], tkEntry);
                             }
                         }
                     }
                 }*/

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
                        //ws.Cells[row, col++].Value = houseEntriesByIsn[csvLine.HausanschlussID].HouseEnergyConsumption;
                        //var diff = csvEnergy - houseEntriesByIsn[csvLine.HausanschlussID].HouseEnergyConsumption;
                        //ws.Cells[row, col].Value = diff;
                    }
                }


                p.SaveAs(new FileInfo(Path.Combine(tgt, "ComparisonCSVvsDB"+suffix + ".xlsx")));
            }
        }

        private void MakeTotalsSankey1([NotNull] [ItemNotNull] List<CSVFile> csvs, [NotNull] ScenarioSliceParameters parameters, [NotNull] string suffix)
        {
            var ssa = new SingleSankeyArrow("EnergyUseTotalVsRealizedInCSV" + suffix, 1000, MyStage, SequenceNumber, Name, Services.Logger, parameters);
            var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
            var monthlyElectricityUsePerStandorts = dbComplexEnergy.Fetch<double>("select YearlyElectricityUseNetz from " + nameof(MonthlyElectricityUsePerStandort));
            const int fac = 1_000_000;
            var totalSum = monthlyElectricityUsePerStandorts.Sum() / fac;

            var sumAssigned = csvs.Sum(x => x.CalculateTotalEnergy()) / fac;
            var sumMissing = totalSum - sumAssigned;
            ssa.AddEntry(new SankeyEntry("Total Electricity", totalSum, 500, Orientation.Straight));
            ssa.AddEntry(new SankeyEntry("Fehlend", sumMissing * -1, 500, Orientation.Straight));
            ssa.AddEntry(new SankeyEntry("In Lastprofilen", sumAssigned * -1, 500, Orientation.Up));
            Services.PlotMaker.MakeSankeyChart(ssa);
        }
        private void HouseCountsPerTrafokreis([NotNull] [ItemNotNull] List<CSVFile> files, [NotNull] ScenarioSliceParameters parameters,
                                              [NotNull] string suffix)
        {
            var column = 0;
            var names = new List<string>();
            var countSeries = new List<BarSeriesEntry>();
            foreach (var pair in files)
            {
                countSeries.Add(BarSeriesEntry.MakeBarSeriesEntry("", pair.Lines.Count, column++));
                names.Add(pair.TsName);
            }

            var filename1 = MakeAndRegisterFullFilename("TrafokreisHouseCount"+ suffix+".png", Name, "", parameters);
            Services.PlotMaker.MakeBarChart(filename1, "", countSeries, names);
            var electricitySeries = new List<BarSeriesEntry>();
            column = 0;
            const double factor = 1_000_000;
            foreach (var pair in files)
            {
                electricitySeries.Add(BarSeriesEntry.MakeBarSeriesEntry("", pair.CalculateTotalEnergy() / factor, column++));
            }

            var filename2 = MakeAndRegisterFullFilename("TrafokreisHouseElectricity"+suffix+".png", Name, "", parameters);
            Services.PlotMaker.MakeBarChart(filename2, "Energie [GWh]", electricitySeries, names);
        }


        protected override void RunChartMaking([NotNull] ScenarioSliceParameters parameters)
        {
            {
                var csvs1 = ReadCSVFiles(parameters, 900, nameof(Z09_CSVExporterGeneration), "Export");
                ProcessAllCharts(csvs1, ".generation");
                var csvs2 = ReadCSVFiles(parameters, 1000, nameof(Z10_CSVExporterLoad), "Export");
                ProcessAllCharts(csvs2, ".load");
            }

            void ProcessAllCharts(List<CSVFile> csvs1, string suffix)
            {
                HouseCountsPerTrafokreis(csvs1, parameters, suffix);
                CheckForDuplicateIDs(csvs1);
                MakeTotalsSankey1(csvs1, parameters, suffix);
                //MakeTotalsSankey2();
                MakeResultExcel(csvs1, parameters, suffix);
            }
        }


        /*
        void MakeAllProfileVisuals()
        {
            var houseProfiles = dbHouse.Fetch<HouseProfile>();
            var tks = houseExportEntries.Select(x => x.Trafokreis).Distinct().ToList();
            foreach (var tk in tks) {
                if (string.IsNullOrWhiteSpace(tk)) {
                    continue;
                }
                var hees = houseExportEntries.Where(x => x.Trafokreis == tk).ToList();
                Profile p = Profile.MakeConstantProfile(0,"Sum",Profile.ProfileResolution.QuarterHour);
                foreach (HouseExportEntry entry in hees) {
                    var houseProfile = houseProfiles.Single(x => x.Guid == entry.Guid);
                    p = p.Add(houseProfile.Profile,"Sum");
                }
                List<BarSeriesEntry> allLs = new List<BarSeriesEntry>();
                var ls1 = p.MakeHourlyAverages().GetBarSeries();
                allLs.Add(ls1);
                var filename = MakeAndRegisterFullFilename(tk + ".svg", "Trafokreise", "");

                Services.PlotMaker.MakeBarChart(filename, "Leistung [kW]", allLs, new List<string>(), ExportType.SVG);
            }
        }

*/


        /*
                    void MakeTotalsSankey2()
                    {
                        var ssa = new SingleSankeyArrow("EnergyUseTotalVsExportEntry", 1000, MyStage, SequenceNumber, Name, Services.Logger, parameters);
                        var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
                        var monthlyElectricityUsePerStandorts = dbComplexEnergy.Fetch<double>("select YearlyElectricityUseNetz from " + nameof(MonthlyElectricityUsePerStandort));
                        const int fac = 1_000_000;
                        var totalSum = monthlyElectricityUsePerStandorts.Sum() / fac;

                        //var sumAssigned = trafoKreisResults.Sum(x => x.TkEnergyEntries.Sum(y => y.HouseEnergyConsumption)) / fac;
                        //var sumMissing = totalSum - sumAssigned;
                        ssa.AddEntry(new SankeyEntry("Total Electricity", totalSum, 500, Orientation.Straight));
                        //ssa.AddEntry(new SankeyEntry("Fehlend", sumMissing * -1, 500, Orientation.Straight));
                        //ssa.AddEntry(new SankeyEntry("In Lastprofilen", sumAssigned * -1, 500, Orientation.Up));
                        Services.PlotMaker.MakeSankeyChart(ssa);
                    }*/
        private static void CheckForDuplicateIDs([NotNull] [ItemNotNull] List<CSVFile> csvs)
        {
            var ids = new HashSet<string>();
            foreach (var trafoKreisResult in csvs) {
                foreach (var entry in trafoKreisResult.Lines) {
                    if (ids.Contains(entry.HausanschlussID)) {
                        throw new Exception("duplicate ID");
                    }

                    ids.Add(entry.HausanschlussID);
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        private static List<CSVFile> ReadCSVFiles([NotNull] ScenarioSliceParameters parameters, int sequence , [NotNull] string exporteRName, [NotNull] string subPath)
        {
            var path = FilenameHelpers.GetTargetDirectory(Stage.ProfileExport, sequence, exporteRName, parameters);
            var di = new DirectoryInfo(Path.Combine(path, subPath));
            if (!di.Exists) {
                throw new FlaException("Directory " + di.FullName + " does not exist");
            }
            var csvfiles = di.GetFiles("*.csv");
            if (csvfiles.Length == 0) {
                throw  new FlaException("No exported files found");
            }
            var csvs = new List<CSVFile>();
            var activeFiles = new List<CSVFile>();
            foreach (var info in csvfiles) {
                var cs = new CSVFile();
                csvs.Add(cs);
                cs.MyThread = new Thread(() => { cs.Read(info); });
                cs.MyThread.Start();
                activeFiles.Add(cs);
                while (activeFiles.Count > 8) {
                    if (activeFiles[0].MyThread == null) {
                        throw new FlaException("thread was null");
                    }
                    activeFiles[0].MyThread.Join();
                    activeFiles.RemoveAt(0);
                }
            }

            while (activeFiles.Count > 0) {
                if (activeFiles[0].MyThread == null)
                {
                    throw new FlaException("thread was null");
                }
                activeFiles[0].MyThread.Join();
                activeFiles.RemoveAt(0);
            }

            return csvs;
        }

        protected override void RunActualProcess(ScenarioSliceParameters parameters)
        {
        }

        public Z11_CSVProfileVisualizer([NotNull] ServiceRepository services)
            : base(nameof(Z11_CSVProfileVisualizer), Stage.ProfileExport, 1100, services, false)
        {
            DevelopmentStatus.Add("Visualize profiles for every tk");
            DevelopmentStatus.Add("Compare energy use from profile with total from houses");
            DevelopmentStatus.Add("Make a pdf report of everything");
        }
    }
}