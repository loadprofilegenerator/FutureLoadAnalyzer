using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using Visualizer;
using ProfileType = Data.DataModel.Profiles.ProfileType;

namespace BurgdorfStatistics._10_ProfileExport {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class Z03_ProfileAnalyzer : RunableForSingleSliceWithBenchmark {
        public Z03_ProfileAnalyzer([NotNull] ServiceRepository services)
            : base(nameof(Z03_ProfileAnalyzer), Stage.ProfileExport, 300, services, false)
        {
            DevelopmentStatus.Add("Implement the other exports too");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            //profile export
            var dbProfileExport = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileExport, parameters);
            //var dbProfileGeneration = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters);
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters);
            var dbHousesPresent = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var prosumers = Prosumer.LoadProsumers(dbProfileExport, TableType.HouseLoad);

            var visualPath = Path.Combine(dbProfileExport.GetResultFullPath(SequenceNumber, Name), "Visual");
            if (!Directory.Exists(visualPath)) {
                Directory.CreateDirectory(visualPath);
            }

            //var tkResults = dbProfileGeneration.Database.Fetch<TrafoKreisResult>();
            var houses = dbHouses.Database.Fetch<House>();
            var houseEnergyUse = dbHousesPresent.Database.Fetch<HouseSummedLocalnetEnergyUse>();
            var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
            var p = new ExcelPackage();
            var ws = p.Workbook.Worksheets.Add("MySheet");
            TrafokreisStatisticsEntry.WriteHeaderToWs(ws);
            int exportRow = 2;
            //Dictionary<long, string> isnTrafokreis = new Dictionary<long, string>();//for checking for duplicate isns
            foreach (var trafokreis in trafokreise) {
                if (string.IsNullOrWhiteSpace(trafokreis)) {
                    continue;
                }

                TrafokreisStatisticsEntry tkse = new TrafokreisStatisticsEntry(trafokreis)
                {
                    OriginalHouses = houses.Where(x => x.TrafoKreis == trafokreis).Count()
                };
                var originalHouseGuids = houses.Where(x => x.TrafoKreis == trafokreis).Select(x => x.HouseGuid).ToList();
                var houseEnergyUses = houseEnergyUse.Where(x => originalHouseGuids.Contains(x.HouseGuid)).ToList();
                foreach (var energyUse in houseEnergyUses) {
                    tkse.OriginalElectricityUse += energyUse.ElectricityUse;
                    tkse.OriginalElectricityUseDay += energyUse.ElectricityUseDayLow;
                    tkse.OriginalElectricityUseNight += energyUse.ElectricityUseNightLow;
                }

                List<double> zeroes = new List<double>(new double[8760 * 4]);
                var trafokreisSumProfile = new Profile(trafokreis, zeroes.AsReadOnly(), ProfileType.Energy);
                var filteredProsumers = prosumers.Where(x => x.TrafoKreis == trafokreis);
                foreach (Prosumer prosumer in filteredProsumers) {
                    if (prosumer.Profile == null) {
                        throw new Exception("Profile was null");
                    }

//                    if (isnTrafokreis.ContainsKey(prosumer.Isn)) {
  //                      throw new Exception("Duplicate ISN");
    //                }
      //              isnTrafokreis.Add(prosumer.Isn,trafokreis);
                    tkse.ProfileElectricityUse += prosumer.Profile.EnergySum();
                    tkse.ProfileDuringNight += prosumer.Profile.EnergyDuringNight();
                    tkse.ProfileDuringDay += prosumer.Profile.EnergyDuringDay();
                    if(Math.Abs(tkse.ProfileDuringNight + tkse.ProfileDuringDay - tkse.ProfileElectricityUse) > 1)
                    {
                        throw new FlaException("Invalid day/night/sum");
                    }
                    tkse.ProfileHouses++;
                    trafokreisSumProfile = trafokreisSumProfile.Add(prosumer.Profile, trafokreis);
                }

                if (Math.Abs(tkse.OriginalElectricityUse) < 1 && tkse.ProfileElectricityUse > 0) {
                    throw new FlaException("No energy planned, but energy allocated");
                }
             /*   if (Math.Abs(trafokreisSumProfile.EnergySum()) < 0.0001 && Math.Abs(tkse.OriginalElectricityUse) > 0.1) {
                    throw new Exception("trafokreis has 0 electricy exported.");
                }*/

                //tkse.CollectedEnergyFromHouses = tkResults.Single(x => x.TrafoKreisName == trafokreis).TotalEnergyAmount;
                tkse.WriteToWorksheet(ws, exportRow++);

                //make trafokreis chart
                var ls = trafokreisSumProfile.GetLineSeriesEntriesList();
                var chartfilename = MakeAndRegisterFullFilename("Visual\\" + trafokreis + ".png", Name, "", parameters);
                FileInfo fi = new FileInfo(chartfilename);
                Debug.Assert(fi.Directory != null, "fi.Directory != null");
                if (!fi.Directory.Exists) {
                    fi.Directory.Create();
                    Thread.Sleep(500);
                }
                Services.PlotMaker.MakeLineChart(chartfilename, trafokreis, ls, new List<PlotMaker.AnnotationEntry>());
            }

            int offset = 12;
            ws.Cells[1, offset].Value = "Summe Stromverbrauch von Localnet";
            ws.Cells[1, offset+1].Formula = "=sum(d:d)/1000000";
            ws.Cells[1, offset+2].Formula = "=SUMIFS(D:D,E:E,\">0\")/1000000";
            ws.Cells[2, offset].Value = "Summe Stromverbrauch in den Exports";
            ws.Cells[2, offset+1].Formula = "=sum(e:e)/1000000";
            ws.Cells[2, offset+2].Formula = "=SUMIFS(E:E,E:E,\">0\")/1000000";

            ws.Cells[3, offset].Value = "Original Nacht Nutzung";
            ws.Cells[3, offset + 1].Formula = "=sum(g:g)/1000000";
            ws.Cells[4, offset].Value = "Original Tag Nutzung";
            ws.Cells[4, offset + 1].Formula = "=sum(h:h)/1000000";
            ws.Cells[5, offset].Value = "Profil Nacht Nutzung";
            ws.Cells[5, offset + 1].Formula = "=sum(I:I)/1000000";
            ws.Cells[6, offset].Value = "Profil Tag Nutzung";
            ws.Cells[6, offset + 1].Formula = "=sum(J:J)/1000000";

            ExcelChart chart = ws.Drawings.AddChart("Comparison", eChartType.ColumnClustered);
            //chart.Title.Text = "Category Chart";
            chart.SetPosition(5, 0, offset, 0);
            chart.SetSize(800, 450);
            chart.Series.Add(ws.Cells["N1:N2"], ws.Cells["L1:L2"]);
            p.SaveAs(new FileInfo(Path.Combine(dbProfileExport.GetResultFullPath(SequenceNumber, Name), "ComparisonExportvsDB.xlsx")));
            p.Dispose();
        }

        protected override void RunChartMaking([NotNull] ScenarioSliceParameters parameters)
        {
            double min = 0;
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            {
                var allLs = new List<LineSeriesEntry>();
                var bkws = dbSrcProfiles.Fetch<BkwProfile>();
                var bkw = bkws[0];
                var ls = bkw.Profile.GetLineSeriesEntry();
                allLs.Add(ls);

                var filename = MakeAndRegisterFullFilename("Profile_BKW.png", Name, "", parameters);

                Services.PlotMaker.MakeLineChart(filename, bkw.Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }

            {
                var dbGeneratedProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters).Database;

                var allLs = new List<LineSeriesEntry>();
                var residual = dbGeneratedProfiles.Fetch<ResidualProfile>();
                if (residual[0].Profile == null) {
                    throw new Exception("Profile was null");
                }

                var ls = residual[0].Profile.GetLineSeriesEntry();
                allLs.Add(ls);
                min = Math.Min(0, residual[0].Profile.Values.Min());

                var filename = MakeAndRegisterFullFilename("Profile_Residual.png", Name, "", parameters);

                Services.PlotMaker.MakeLineChart(filename, residual[0].Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }

            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            foreach (var rlm in rlms) {
                var allLs = new List<LineSeriesEntry>();

                var ls1 = rlm.Profile.GetLineSeriesEntry();
                allLs.Add(ls1);

                var filename = MakeAndRegisterFullFilename("Profile." + rlm.Name + ".png", Name, "", parameters);
                min = Math.Min(0, rlm.Profile.Values.Min());
                Services.PlotMaker.MakeLineChart(filename, rlm.Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }
        }
    }

    public class TrafokreisStatisticsEntry {
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public TrafokreisStatisticsEntry()
        {
        }

        public TrafokreisStatisticsEntry([NotNull] string name) => Name = name;

        [NotNull]
        public string Name { get; set; }
        public double OriginalElectricityUse { get; set; }
        public double OriginalElectricityUseDay { get; set; }
        public double OriginalElectricityUseNight { get; set; }
        public int OriginalHouses { get; set; }
        public double ProfileElectricityUse { get; set; }
        public int ProfileHouses { get; set; }
        public double CollectedEnergyFromHouses { get; set; }
        public double ProfileDuringNight { get; set; }
        public double ProfileDuringDay { get; set; }

        public static void WriteHeaderToWs([NotNull] ExcelWorksheet ws)
        {
            int col = 1;
            ws.Cells[1, col++].Value = "Name";
            ws.Cells[1, col++].Value = "Original Houses";
            ws.Cells[1, col++].Value = "Exported Houses";
            ws.Cells[1, col++].Value = "Original Energy";
            ws.Cells[1, col++].Value = "Exported Energy";
            ws.Cells[1, col++].Value = "Planned in TKResults";
            ws.Cells[1, col++].Value = "Original Night Use";
            ws.Cells[1, col++].Value = "Original Day Use";
            ws.Cells[1, col++].Value = "Profile Night Use";
            // ReSharper disable once RedundantAssignment
            ws.Cells[1, col++].Value = "Profile Day Use";
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void WriteToWorksheet([NotNull] ExcelWorksheet ws, int row)
        {
            int col = 1;
            ws.Cells[row, col++].Value = Name;
            ws.Cells[row, col++].Value = OriginalHouses;
            ws.Cells[row, col++].Value = ProfileHouses;
            ws.Cells[row, col++].Value = OriginalElectricityUse;
            ws.Cells[row, col++].Value = ProfileElectricityUse;
            ws.Cells[row, col++].Value = CollectedEnergyFromHouses;
            ws.Cells[row, col++].Value = OriginalElectricityUseNight;
            ws.Cells[row, col++].Value = OriginalElectricityUseDay;
            ws.Cells[row, col++].Value = ProfileDuringNight;
            ws.Cells[row, col++].Value = ProfileDuringDay;
            ws.Cells[row, col++].Value = ProfileDuringDay;
        }
    }
}