using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using ServiceRepository = BurgdorfStatistics.Tooling.ServiceRepository;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class A07_TrafokreisAnalyzer : RunableWithBenchmark {
        public A07_TrafokreisAnalyzer([NotNull] ServiceRepository services)
            : base(nameof(A07_TrafokreisAnalyzer), Stage.Houses, 7, services, true)
        {
        }

        protected override void RunActualProcess()
        {
        }

        protected override void RunChartMaking()
        {
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var energyUses = dbHouse.Fetch<HouseSummedLocalnetEnergyUse>();
            var households = dbHouse.Fetch<Household>();
            MakeTrafoKreisMapForNonTrafoKreisHouses(Constants.PresentSlice);
            MakeTrafoKreisMap(Constants.PresentSlice);
            MakeTrafoKreisMapForElectricityUsers(Constants.PresentSlice);
            MakeBarChartEnergyPertrafokreis(Constants.PresentSlice);
            MakeBarChartHouseholdsPertrafokreis(Constants.PresentSlice);
            MakeBarChartHousesPerTrafokreis(Constants.PresentSlice);

            MakeSankeyEnergyChartPertrafokreis(Constants.PresentSlice);

            void MakeSankeyEnergyChartPertrafokreis(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                //List<string> categoryLabels = new List<string>();
                var energyPerTk = new Dictionary<string, double>();
                for (var i = 0; i < trafokreise.Count; i++) {
                    var tk = trafokreise[i];
                    var tkHouses = houses.Where(x => x.TrafoKreis == tk).ToList();
                    double sumElectricity = 0;
                    foreach (var tkHouse in tkHouses) {
                        var eu = energyUses.Single(x => x.HouseGuid == tkHouse.HouseGuid);
                        sumElectricity += eu.ElectricityUse;
                    }

                    //  categoryLabels.Add(tk);
                    var tkSp = tk ?? "";
                    if (energyPerTk.ContainsKey(tkSp)) {
                        energyPerTk[tkSp] += sumElectricity;
                    }
                    else {
                        energyPerTk.Add(tkSp, sumElectricity);
                    }
                }

                var energysum = energyPerTk.Values.Sum() / 1000000;
                var energySumAssigned = energyPerTk.Where(x => !string.IsNullOrWhiteSpace(x.Key)).Select(x => x.Value).Sum() / 1000000;
                var diff = energysum - energySumAssigned;
                var ssa = new SingleSankeyArrow("EnergyInAssignedTks", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Strom Gesamt", energysum, 200, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("An Trafokreisen", energySumAssigned * -1, 200, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Nicht an Trafokreisen", diff * -1, 200, Orientation.Down));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakeBarChartHousesPerTrafokreis(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                var barseries = new List<BarSeriesEntry>();

                var categoryLabels = new List<string>();
                for (var i = 0; i < trafokreise.Count; i++) {
                    var tk = trafokreise[i];
                    var tkHouses = houses.Where(x => x.TrafoKreis == tk).ToList();
                    categoryLabels.Add(tk);
                    var bse = BarSeriesEntry.MakeBarSeriesEntry(tk, tkHouses.Count, i);
                    barseries.Add(bse);
                }

                var filename = MakeAndRegisterFullFilename("HousesProTrafokreis.png", Name, "", slice);
                Services.PlotMaker.MakeBarChart(filename, "", barseries, categoryLabels);
            }

            void MakeBarChartHouseholdsPertrafokreis( ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                var barseries = new List<BarSeriesEntry>();

                var categoryLabels = new List<string>();
                for (var i = 0; i < trafokreise.Count; i++) {
                    var tk = trafokreise[i];
                    var tkHouses = houses.Where(x => x.TrafoKreis == tk).ToList();
                    var tkhouseholdCount = 0;
                    foreach (var tkHouse in tkHouses) {
                        var householdsCount = households.Where(x => x.HouseGuid == tkHouse.HouseGuid).Count();
                        tkhouseholdCount += householdsCount;
                    }

                    categoryLabels.Add(tk);
                    var bse = BarSeriesEntry.MakeBarSeriesEntry(tk, tkhouseholdCount, i);
                    barseries.Add(bse);
                }

                var filename = MakeAndRegisterFullFilename("HouseholdsProTrafokreis.png", Name, "", slice);
                Services.PlotMaker.MakeBarChart(filename, "", barseries, categoryLabels);
            }

            void MakeBarChartEnergyPertrafokreis(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                var barseries = new List<BarSeriesEntry>();
                var filenameCsv = MakeAndRegisterFullFilename("StromProTrafokreis.csv", Name, "", slice);
                using (var sw = new StreamWriter(filenameCsv)) {
                    var categoryLabels = new List<string>();
                    for (var i = 0; i < trafokreise.Count; i++) {
                        var tk = trafokreise[i];
                        var tkHouses = houses.Where(x => x.TrafoKreis == tk).ToList();
                        double sumElectricity = 0;
                        foreach (var tkHouse in tkHouses) {
                            var eu = energyUses.Single(x => x.HouseGuid == tkHouse.HouseGuid);
                            sumElectricity += eu.ElectricityUse;
                        }

                        categoryLabels.Add(tk);
                        var bse = BarSeriesEntry.MakeBarSeriesEntry(tk, sumElectricity, i);
                        barseries.Add(bse);
                        sw.WriteLine(tk + ";" + sumElectricity);
                    }

                    var filename = MakeAndRegisterFullFilename("StromProTrafokreis.png", Name, "", slice);
                    Services.PlotMaker.MakeBarChart(filename, "", barseries, categoryLabels);
                    sw.Close();
                }
            }


            void MakeTrafoKreisMap(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                var cg = new ColorGenerator();

                RGB GetColor(House h)
                {
                    var idx = trafokreise.IndexOf(h.TrafoKreis);
                    return cg.GetRGB(idx);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("Trafokreise.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }

            void MakeTrafoKreisMapForElectricityUsers(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                var cg = new ColorGenerator();

                RGB GetColor(House h)
                {
                    if (h.GebäudeObjectIDs.Count == 0) {
                        return new RGB(240, 240, 240);
                    }

                    var idx = trafokreise.IndexOf(h.TrafoKreis);
                    return cg.GetRGB(idx);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("TrafokreiseEbbeLightgrey.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }

            void MakeTrafoKreisMapForNonTrafoKreisHouses(ScenarioSliceParameters slice)
            {
                RGBWithSize GetColor(House h)
                {
                    var eu = energyUses.Single(x => x.HouseGuid == h.HouseGuid);
                    var energy = eu.ElectricityUse / 1_000_000;
                    if (energy < 10) {
                        energy = 10;
                    }

                    if (string.IsNullOrWhiteSpace(h.TrafoKreis)) {
                        return new RGBWithSize(255, 0, 0, (int)energy);
                    }

                    return new RGBWithSize(0, 0, 0, 10);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("HousesWithoutTrafoKreisEnergyUse.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }
    }
}