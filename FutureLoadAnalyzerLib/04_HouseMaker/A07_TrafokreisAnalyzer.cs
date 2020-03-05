using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using ServiceRepository = FutureLoadAnalyzerLib.Tooling.ServiceRepository;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
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
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var energyUses = dbHouse.Fetch<HouseSummedLocalnetEnergyUse>();
            var households = dbHouse.Fetch<Household>();
            MakeTrafoKreisMapForNonTrafoKreisHouses(Constants.PresentSlice);
            MakeTrafoKreisOsmMap(Constants.PresentSlice);
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
                        var eu = energyUses.Single(x => x.HouseGuid == tkHouse.Guid);
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
                var ssa = new SingleSankeyArrow("EnergyInAssignedTks", 500, MyStage,
                    SequenceNumber, Name, slice, Services);
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

                var filename = MakeAndRegisterFullFilename("HousesProTrafokreis.png", slice);
                Services.PlotMaker.MakeBarChart(filename, "HousesProTrafokreis", barseries, categoryLabels);
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
                        var householdsCount = households.Count(x => x.HouseGuid == tkHouse.Guid);
                        tkhouseholdCount += householdsCount;
                    }

                    categoryLabels.Add(tk);
                    var bse = BarSeriesEntry.MakeBarSeriesEntry(tk, tkhouseholdCount, i);
                    barseries.Add(bse);
                }

                var filename = MakeAndRegisterFullFilename("HouseholdsProTrafokreis.png", slice);
                Services.PlotMaker.MakeBarChart(filename, "HouseholdsProTrafokreis", barseries, categoryLabels);
            }

            void MakeBarChartEnergyPertrafokreis(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();
                var barseries = new List<BarSeriesEntry>();
                var filenameCsv = MakeAndRegisterFullFilename("StromProTrafokreis.csv", slice);
                using (var sw = new StreamWriter(filenameCsv)) {
                    var categoryLabels = new List<string>();
                    for (var i = 0; i < trafokreise.Count; i++) {
                        var tk = trafokreise[i];
                        var tkHouses = houses.Where(x => x.TrafoKreis == tk).ToList();
                        double sumElectricity = 0;
                        foreach (var tkHouse in tkHouses) {
                            var eu = energyUses.Single(x => x.HouseGuid == tkHouse.Guid);
                            sumElectricity += eu.ElectricityUse;
                        }

                        categoryLabels.Add(tk);
                        var bse = BarSeriesEntry.MakeBarSeriesEntry(tk, sumElectricity, i);
                        barseries.Add(bse);
                        sw.WriteLine(tk + ";" + sumElectricity);
                    }

                    var filename = MakeAndRegisterFullFilename("StromProTrafokreis.png", slice);
                    Services.PlotMaker.MakeBarChart(filename, "StromProTrafokreis", barseries, categoryLabels);
                    sw.Close();
                }
            }


            void MakeTrafoKreisMap(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();

                RGB GetColor(House h)
                {
                    var idx = trafokreise.IndexOf(h.TrafoKreis);
                    return ColorGenerator.GetRGB(idx);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("Trafokreise.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
            void MakeTrafoKreisOsmMap(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();

                Dictionary<string, RGB> colorByTrafokreis = new Dictionary<string, RGB>();
                int idx = 0;
                foreach (var tk in trafokreise) {
                    var rgb = ColorGenerator.GetRGB(idx);
                    while (rgb.R == 255 && rgb.G == 255 && rgb.B == 255) {
                        idx++;
                        rgb = ColorGenerator.GetRGB(idx);
                    }
                    colorByTrafokreis.Add(tk,rgb);
                    idx++;

                }

                foreach (var rgb in colorByTrafokreis) {
                    Info(rgb.Key + ": " + rgb.Value);
                }
                RGB GetColor(House h) => colorByTrafokreis[h.TrafoKreis ?? throw new InvalidOperationException()];

                var mapPoints = houses.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("TrafokreiseOsm.png", slice);
                var legendEntries = new List<MapLegendEntry>();
                legendEntries.Add(new MapLegendEntry("Mittelspannung", colorByTrafokreis["MV_customers"]));
                legendEntries.Add(new MapLegendEntry("Nicht erfasst", new RGB(255,0,0)));
                Services.PlotMaker.MakeOsmMap( Name, filename, mapPoints,new List<WgsPoint>(),
                    legendEntries,new List<LineEntry>());
            }

            void MakeTrafoKreisMapForElectricityUsers(ScenarioSliceParameters slice)
            {
                var trafokreise = houses.Select(x => x.TrafoKreis).Distinct().ToList();

                RGB GetColor(House h)
                {
                    if (h.GebäudeObjectIDs.Count == 0) {
                        return new RGB(240, 240, 240);
                    }

                    var idx = trafokreise.IndexOf(h.TrafoKreis);
                    return ColorGenerator.GetRGB(idx);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("TrafokreiseEbbeLightgrey.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }

            void MakeTrafoKreisMapForNonTrafoKreisHouses(ScenarioSliceParameters slice)
            {
                RGBWithSize GetColor(House h)
                {
                    var eu = energyUses.Single(x => x.HouseGuid == h.Guid);
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
                var filename = MakeAndRegisterFullFilename("HousesWithoutTrafoKreisEnergyUse.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}