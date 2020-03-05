using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using MathNet.Numerics.Statistics;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Visualisation.SingleSlice {
    public class HeatingSystemCharts : VisualisationBase {
        public HeatingSystemCharts([NotNull] ServiceRepository services, Stage myStage) : base(nameof(HeatingSystemCharts), services, myStage)
        {
            DevelopmentStatus.Add("Maps are messed up");
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            List<House> houses1 = dbHouse.Fetch<House>();
            var heatingsystems1 = dbHouse.Fetch<HeatingSystemEntry>();
            Dictionary<string, HeatingSystemEntry> heatingsystemsByGuid = new Dictionary<string, HeatingSystemEntry>();
            foreach (var hse in heatingsystems1) {
                heatingsystemsByGuid.Add(hse.HouseGuid, hse);
            }

            MakeHeatingSystemAnalysis();
            EnergyIntensityHistogram();
            MakeHeatingTypeIntensityMap();
            MakeHeatingTypeMap();
            MakeHeatingSystemSankey();
            HeatingSystemCountHistogram();
            MakeHeatingSystemMapError();
            MakeHeatingSystemMap();
            MakeFernwärmeTypeMap();
            if (isPresent) {
                PresentOnlyVisualisations(heatingsystemsByGuid, slice, houses1, dbHouse);
            }

            void MakeHeatingSystemAnalysis()
            {
                var filename = MakeAndRegisterFullFilename("analysis.csv", slice);
                var sw = new StreamWriter(filename);
                sw.WriteLine("Original Heating system - Anzahl");
                foreach (HeatingSystemType hst in Enum.GetValues(typeof(HeatingSystemType))) {
                    var fs1 = heatingsystems1.Where(x => x.OriginalHeatingSystemType == hst).ToList();
                    sw.WriteLine(hst + ";" + fs1.Count);
                }

                sw.WriteLine("");
                sw.WriteLine("");
                sw.WriteLine("Original Heating system - Summe");
                foreach (HeatingSystemType hst in Enum.GetValues(typeof(HeatingSystemType))) {
                    var fs1 = heatingsystems1.Where(x => x.OriginalHeatingSystemType == hst).ToList();
                    sw.WriteLine(hst + ";" + fs1.Sum(x => x.EffectiveEnergyDemand));
                }

                sw.WriteLine("");
                sw.WriteLine("");

                sw.WriteLine("Target Anzahl");
                foreach (HeatingSystemType hst in Enum.GetValues(typeof(HeatingSystemType))) {
                    var fs1 = heatingsystems1.Where(x => x.SynthesizedHeatingSystemType == hst).ToList();
                    sw.WriteLine(hst + ";" + fs1.Count);
                }

                sw.WriteLine("");
                sw.WriteLine("");

                sw.WriteLine("Target Summe");
                foreach (HeatingSystemType hst in Enum.GetValues(typeof(HeatingSystemType))) {
                    var fs1 = heatingsystems1.Where(x => x.SynthesizedHeatingSystemType == hst).ToList();
                    sw.WriteLine(hst + ";" + fs1.Sum(x => x.EffectiveEnergyDemand));
                }

                sw.Close();
            }

            void EnergyIntensityHistogram()
            {
                var filename = MakeAndRegisterFullFilename("EnergyIntensityHistogram.png", slice);
                var ages = heatingsystems1.Select(x => x.CalculatedAverageHeatingEnergyDemandDensity).Where(y => y > 0).ToList();
                var barSeries = new List<BarSeriesEntry>();
                var h = new Histogram(ages, 100);
                barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(h, out var colnames, "F0"));
                Services.PlotMaker.MakeBarChart(filename, "EnergyIntensityHistogram", barSeries, colnames);
                var xlsfilename = MakeAndRegisterFullFilename("EnergyIntensity.xlsx", slice);
                var heatingSystems = heatingsystems1.Where(y => y.CalculatedAverageHeatingEnergyDemandDensityWithNulls != null).ToList();
                var rbDict = new Dictionary<string,RowBuilder>();
                foreach (var hs in heatingSystems) {
                    int bucket = (int)(Math.Floor(hs.CalculatedAverageHeatingEnergyDemandDensityWithNulls / 50 ?? 0 ) * 50);
                    string key = bucket.ToString();
                    string et = hs.EnergyType.ToString();
                    RowBuilder rb;
                    if (rbDict.ContainsKey(key)) {
                        rb = rbDict[key];
                    }
                    else {
                        rb = RowBuilder.Start("Energieträger", key);
                        rbDict.Add(key, rb);
                    }

                    rb.AddToPossiblyExisting(et, 1);

                }
                RowCollection rc = new RowCollection("energy","energy");
                foreach (var builder in rbDict) {
                    rc.Add(builder.Value);
                }
                RowCollection rc2 = new RowCollection("raw","raw");
                foreach (var heatingSystemEntry in heatingsystems1) {
                    RowBuilder rb = RowBuilder.Start("Träger",heatingSystemEntry.EnergyType)
                        .Add("Effektiv",heatingSystemEntry.EffectiveEnergyDemand)
                        .Add("EBF",heatingSystemEntry.Ebf);
                    rc2.Add(rb);
                }
                XlsxDumper.WriteToXlsx(xlsfilename,rc, rc2);
            }

            void MakeHeatingSystemSankey()
            {
                var ssa1 = new SingleSankeyArrow("HouseHeatingSystems", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa1.AddEntry(new SankeyEntry("Houses", houses1.Count, 5000, Orientation.Straight));
                var ssa2 = new SingleSankeyArrow("EnergyBySystems", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa2.AddEntry(new SankeyEntry("Houses", heatingsystems1.Sum(x => x.EffectiveEnergyDemand) / 1000000, 5000, Orientation.Straight));
                var counts = new Dictionary<HeatingSystemType, int>();
                var energy = new Dictionary<HeatingSystemType, double>();
                foreach (var entry in heatingsystems1) {
                    if (!counts.ContainsKey(entry.SynthesizedHeatingSystemType)) {
                        counts.Add(entry.SynthesizedHeatingSystemType, 0);
                        energy.Add(entry.SynthesizedHeatingSystemType, 0);
                    }

                    counts[entry.SynthesizedHeatingSystemType]++;
                    energy[entry.SynthesizedHeatingSystemType] += entry.EffectiveEnergyDemand;
                }

                var i = 1;
                foreach (var pair in counts) {
                    ssa1.AddEntry(new SankeyEntry(pair.Key.ToString(), pair.Value * -1, 2000 * i, Orientation.Up));
                    i++;
                }

                i = 1;
                foreach (var pair in energy) {
                    ssa2.AddEntry(new SankeyEntry(pair.Key.ToString(), pair.Value * -1 / 1000000, 2000 * i, Orientation.Up));
                    i++;
                }

                Services.PlotMaker.MakeSankeyChart(ssa1);
                Services.PlotMaker.MakeSankeyChart(ssa2);
            }

            void HeatingSystemCountHistogram()
            {
                var counts = new Dictionary<HeatingSystemType, int>();
                foreach (var entry in heatingsystems1) {
                    if (!counts.ContainsKey(entry.SynthesizedHeatingSystemType)) {
                        counts.Add(entry.SynthesizedHeatingSystemType, 0);
                    }

                    counts[entry.SynthesizedHeatingSystemType]++;
                }

                var filename = MakeAndRegisterFullFilename("HeatingSystemHistogram.png", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                var column = 0;
                foreach (var pair in counts) {
                    names.Add(pair.Value.ToString());
                    var count = pair.Value;
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(pair.Key.ToString(), count, column));
                    column++;
                }

                Services.PlotMaker.MakeBarChart(filename, "HeatingSystemHistogram", barSeries, names);
            }

            void MakeHeatingSystemMapError()
            {
                RGB GetColor(House h)
                {
                    var hse = heatingsystemsByGuid[h.Guid];
                    if (hse.OriginalHeatingSystemType == HeatingSystemType.Fernwärme) {
                        return Constants.Red;
                    }

                    if (hse.OriginalHeatingSystemType == HeatingSystemType.Gas) {
                        return Constants.Orange;
                    }

                    if (hse.OriginalHeatingSystemType == HeatingSystemType.FeuerungsstättenGas) {
                        return Constants.Orange;
                    }

                    return Constants.Black;
                }

                var mapPoints = houses1.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("KantonHeatingSystemErrors.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Kanton Fernwärme", Constants.Red),
                    new MapLegendEntry("Kanton Gas", Constants.Orange)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }

            void MakeHeatingSystemMap()
            {
                var rgbs = new Dictionary<HeatingSystemType, RGB>();
                var hs = heatingsystems1.Select(x => x.SynthesizedHeatingSystemType).Distinct().ToList();
                var idx = 0;
                foreach (var type in hs) {
                    rgbs.Add(type, ColorGenerator.GetRGB(idx++));
                }

                RGB GetColor(House h)
                {
                    var hse = heatingsystemsByGuid[h.Guid];
                    return rgbs[hse.SynthesizedHeatingSystemType];
                }

                var mapPoints = houses1.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("HeatingSystemMap.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in rgbs) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }


            void MakeHeatingTypeMap()
            {
                var maxEnergy = heatingsystems1.Max(x => x.EffectiveEnergyDemand);
                var colorsByHeatingSystem = new Dictionary<HeatingSystemType, RGB> {
                    {
                        HeatingSystemType.Gas, Constants.Orange
                    }, {
                        HeatingSystemType.Öl, Constants.Black
                    }, {
                        HeatingSystemType.Electricity, Constants.Green
                    }, {
                        HeatingSystemType.Heatpump, Constants.Green
                    }, {
                        HeatingSystemType.Fernwärme, Constants.Blue
                    }, {
                        HeatingSystemType.Other, Constants.Türkis
                    }, {
                        HeatingSystemType.None, Constants.Yellow
                    }
                };

                RGBWithSize GetColorWithSize(House h)
                {
                    var s = heatingsystemsByGuid[h.Guid];
                    var energy = Math.Log(s.EffectiveEnergyDemand / maxEnergy * 100) * 50;
                    if (energy < 10) {
                        energy = 10;
                    }

                    if (!colorsByHeatingSystem.ContainsKey(s.SynthesizedHeatingSystemType)) {
                        throw new Exception("undefined color for " + s.SynthesizedHeatingSystemType);
                    }

                    var rgb = colorsByHeatingSystem[s.SynthesizedHeatingSystemType];
                    return new RGBWithSize(rgb.R, rgb.G, rgb.B, (int)energy);
                }

                RGB GetColor(House h)
                {
                    var s = heatingsystemsByGuid[h.Guid];

                    if (!colorsByHeatingSystem.ContainsKey(s.SynthesizedHeatingSystemType)) {
                        throw new Exception("undefined color for " + s.SynthesizedHeatingSystemType);
                    }

                    var rgb = colorsByHeatingSystem[s.SynthesizedHeatingSystemType];
                    return new RGB(rgb.R, rgb.G, rgb.B);
                }

                var mapPoints = houses1.Select(x => x.GetMapPointWithSize(GetColorWithSize)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHeatingTypeAndSystemPerHousehold.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in colorsByHeatingSystem) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
                var filenameOsm = MakeAndRegisterFullFilename("MapHeatingTypeAndSystemPerHouseholdOsm.png", slice);
                legendEntries.Add(new MapLegendEntry("Nicht Stadtgebiet", Constants.Red));
                var mceh = houses1.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                Services.PlotMaker.MakeOsmMap("HeatingTypeMap", filenameOsm, mceh, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }

            void MakeFernwärmeTypeMap()
            {
                RGBWithLabel GetColor(House h)
                {
                    var s = heatingsystemsByGuid[h.Guid];
                    if (s.SynthesizedHeatingSystemType == HeatingSystemType.Fernwärme) {
                        return new RGBWithLabel(Constants.Green, "");
                    }

                    return new RGBWithLabel(Constants.Blue, ""); //h.ComplexName
                }

                var legendEntries = new List<MapLegendEntry>();
                var filenameOsm = MakeAndRegisterFullFilename("FernwärmeOSM.png", slice);
                legendEntries.Add(new MapLegendEntry("Nicht Stadtgebiet", Constants.Red));
                legendEntries.Add(new MapLegendEntry("Nicht Fernwärme", Constants.Blue));
                legendEntries.Add(new MapLegendEntry("Fernwärme", Constants.Green));
                var mceh = houses1.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                Services.PlotMaker.MakeOsmMap("HeatingTypeMap", filenameOsm, mceh, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }

            void MakeHeatingTypeIntensityMap()
            {
                var colorsByHeatingSystem = new Dictionary<HeatingSystemType, RGB> {
                    {
                        HeatingSystemType.Gas, Constants.Orange
                    }, {
                        HeatingSystemType.Öl, Constants.Black
                    }, {
                        HeatingSystemType.Electricity, Constants.Green
                    }, {
                        HeatingSystemType.Heatpump, Constants.Green
                    }, {
                        HeatingSystemType.Fernwärme, Constants.Blue
                    }, {
                        HeatingSystemType.Other, Constants.Türkis
                    }, {
                        HeatingSystemType.None, Constants.Türkis
                    }
                };

                RGBWithSize GetColor(House h)
                {
                    var s = heatingsystemsByGuid[h.Guid];
                    var energy = s.CalculatedAverageHeatingEnergyDemandDensity / 10;
                    if (energy < 10) {
                        energy = 10;
                    }

                    if (!colorsByHeatingSystem.ContainsKey(s.SynthesizedHeatingSystemType)) {
                        throw new Exception("undefined color for " + s.SynthesizedHeatingSystemType);
                    }

                    var rgb = colorsByHeatingSystem[s.SynthesizedHeatingSystemType];
                    return new RGBWithSize(rgb.R, rgb.G, rgb.B, (int)energy);
                }

                var mapPoints = houses1.Select(x => x.GetMapPointWithSize(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHeatingTypeAndIntensityPerHouse.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in colorsByHeatingSystem) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }

        private void PresentOnlyVisualisations([NotNull] Dictionary<string, HeatingSystemEntry> heatingsystemsByGuid,
                                               [NotNull] ScenarioSliceParameters slice,
                                               [NotNull] [ItemNotNull] List<House> houses1,
                                               [NotNull] MyDb dbHouse)
        {
            MakeHeatingSystemMapForFeuerungstätten1();
            MakeHeatingSystemMapForFeuerungstätten2();
            MakeHeatingSystemMapForEbbe();

            void MakeHeatingSystemMapForFeuerungstätten1()
            {
                RGB GetColor(House h)
                {
                    var hse = heatingsystemsByGuid[h.Guid];
                    if (!string.IsNullOrWhiteSpace(hse.FeuerungsstättenType)) {
                        return Constants.Green;
                    }

                    return Constants.Black;
                }


                var filename = MakeAndRegisterFullFilename("AllHousesWithFeuerungsstättenData.png", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Feuerungsstätten-Daten vorhanden", Constants.Green),
                    new MapLegendEntry("Feuerungsstätten-Daten nicht vorhanden", Constants.Black)
                };
                var mceh = houses1.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                Services.PlotMaker.MakeOsmMap("HeatingTypeMap", filename, mceh, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }

            void MakeHeatingSystemMapForEbbe()
            {
                var houseHeatingMethods = dbHouse.Fetch<HouseHeating>();

                RGB GetColor(House h)
                {
                    var houseHeatingMethod = houseHeatingMethods.Single(x => x.HouseGuid == h.Guid);
                    if (houseHeatingMethod.KantonHeizungEnergyDemand > 0) {
                        return Constants.Green;
                    }

                    return Constants.Black;
                }

                var filename = MakeAndRegisterFullFilename("AllHousesWithEbbeData.png", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Ebbe-Daten vorhanden", Constants.Green),
                    new MapLegendEntry("Feuerungsstätten-Daten nicht vorhanden", Constants.Black)
                };
                var mceh = houses1.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                Services.PlotMaker.MakeOsmMap("HeatingTypeMap", filename, mceh, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }

            MakeHeatingSystemMapForNoBeco();

            void MakeHeatingSystemMapForNoBeco()
            {
                var houseHeatingMethods = dbHouse.Fetch<HouseHeating>();

                RGB GetColor(House h)
                {
                    var houseHeatingMethod = houseHeatingMethods.Single(x => x.HouseGuid == h.Guid);
                    var hse = heatingsystemsByGuid[h.Guid];
                    if (string.IsNullOrWhiteSpace(hse.FeuerungsstättenType) && houseHeatingMethod.KantonHeatingMethods.Count > 0) {
                        return Constants.Blue;
                    }

                    if (!string.IsNullOrWhiteSpace(hse.FeuerungsstättenType) && houseHeatingMethod.KantonHeatingMethods.Count > 0) {
                        return Constants.Green;
                    }

                    return Constants.Black;
                }

                var filename = MakeAndRegisterFullFilename("AllHousesWithEbbeDataNoBeco.png", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Ebbe-Daten & Beco vorhanden,", Constants.Green),
                    new MapLegendEntry("Ebbe-Daten & kein Beco vorhanden,", Constants.Blue),
                    new MapLegendEntry("Feuerungsstätten-Daten nicht vorhanden", Constants.Black)
                };
                var mceh = houses1.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                Services.PlotMaker.MakeOsmMap("HeatingTypeMap", filename, mceh, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }

            void MakeHeatingSystemMapForFeuerungstätten2()
            {
                RGB GetColor(House h)
                {
                    var hse = heatingsystemsByGuid[h.Guid];
                    if (hse.FeuerungsstättenType == "Gas") {
                        return Constants.Green;
                    }

                    if (hse.FeuerungsstättenType == "Oel") {
                        return Constants.Blue;
                    }

                    if (hse.FeuerungsstättenType == "Oel,Gas") {
                        return Constants.Türkis;
                    }

                    if (!string.IsNullOrWhiteSpace(hse.FeuerungsstättenType)) {
                        throw new FlaException("Unbekannter Beco typ: " + hse.FeuerungsstättenType);
                    }

                    return Constants.Black;
                }


                var filename = MakeAndRegisterFullFilename("AllHousesWithFeuerungstättenByType.png", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Beco Gas", Constants.Green),
                    new MapLegendEntry("Beco Öl", Constants.Blue),
                    new MapLegendEntry("Beco Öl/Gas", Constants.Türkis),
                    new MapLegendEntry("Feuerungsstätten-Daten nicht vorhanden", Constants.Black)
                };
                var mceh = houses1.Select(x => x.GetMapColorForHouse(GetColor)).ToList();
                Services.PlotMaker.MakeOsmMap("HeatingTypeMap", filename, mceh, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }
        }
    }
}