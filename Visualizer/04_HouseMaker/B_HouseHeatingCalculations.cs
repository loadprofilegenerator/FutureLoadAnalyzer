using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// this class collects the existing heating information
    /// </summary>
    internal class B_HouseHeatingCalculations : RunableWithBenchmark {
        public B_HouseHeatingCalculations([NotNull] ServiceRepository services)
            : base(nameof(B_HouseHeatingCalculations), Stage.Houses, 200, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<HouseHeating>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;

            var buildingcomplexes = dbComplex.Fetch<BuildingComplex>();
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var energieBedarfsDatenBern = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            var houses = dbHouses.Fetch<House>();
            var potentialHeatingSystems = dbHouses.Fetch<PotentialHeatingSystemEntry>();
            dbHouses.BeginTransaction();
            foreach (var h in houses) {
                var c = buildingcomplexes.First(x => x.ComplexID == h.ComplexID);
                var hs = potentialHeatingSystems.Where(x => x.HouseGuid == h.HouseGuid).ToList();
                var hh = new HouseHeating {
                    LocalnetFernwärmeEnergyUse = hs.Sum(x => x.YearlyFernwärmeDemand),
                    LocalnetGasEnergyUse = hs.Sum(x => x.YearlyGasDemand),
                    HouseGuid = h.HouseGuid,
                    LocalnetHeatingSystemEntryCount = hs.Count
                };
                //collect ebbe daten
                foreach (var eGid in c.EGids) {
                    var ebdbs = energieBedarfsDatenBern.Where(x => x.egid == eGid).ToList();
                    if (ebdbs.Count > 1) {
                        throw new Exception("too many ebdb");
                    }

                    if (ebdbs.Count == 1) {
                        var eb = ebdbs[0];
                        hh.KantonHeatingMethods.Add(GetHeatingMethodString((int)eb.upd_genhz));
                        hh.KantonTotalEnergyDemand += eb.calc_ehzww;
                        hh.KantonHeizungEnergyDemand += eb.calc_ehz;
                        hh.KantonWarmwasserEnergyDemand += eb.calc_eww;
                        hh.KantonDhwMethods.Add(GetHeatingMethodString(eb.upd_genww));
                    }
                }

                hh.LocalnetCombinedEnergyDemand = hh.LocalnetFernwärmeEnergyUse + hh.LocalnetGasEnergyUse;
                //hh.LocalnetAdjustedHeatingDemand =(hh.LocalnetFernwärmeEnergyUse / 0.95) + (hh.LocalnetGasEnergyUse / 0.85);
                hh.LocalnetHeatingEnergyDensity = hh.LocalnetAdjustedHeatingDemand / h.EnergieBezugsFläche;
                if (hh.LocalnetHeatingEnergyDensity > 500) {
                    hh.LocalnetHeatingEnergyDensity = 250;
                    hh.EnergyDensityIndustrialApplication = hh.LocalnetHeatingEnergyDensity - 250;
                }

                hh.KantonHeatingEnergyDensity = hh.KantonTotalEnergyDemand / h.EnergieBezugsFläche;
                if (Math.Abs(hh.LocalnetHeatingEnergyDensity) > 0.001) {
                    hh.HeatingEnergyDifference = hh.LocalnetHeatingEnergyDensity - hh.KantonHeatingEnergyDensity;
                    if (double.IsNaN(hh.HeatingEnergyDifference) || double.IsInfinity(hh.HeatingEnergyDifference)) {
                        hh.HeatingEnergyDifference = 0;
                    }
                }

                if (hh.LocalnetHeatingEnergyDensity > 0) {
                    hh.MergedHeatingEnergyDensity = hh.LocalnetHeatingEnergyDensity;
                }
                else {
                    hh.MergedHeatingEnergyDensity = hh.KantonHeatingEnergyDensity;
                }

                if (hh.MergedHeatingEnergyDensity < 0) {
                    throw new Exception("Negative heating intensity");
                }

                hh.MergedHeatingDemand = 0;
                if (hh.LocalnetCombinedEnergyDemand > 1) {
                    hh.MergedHeatingDemand = hh.LocalnetCombinedEnergyDemand;
                }
                else {
                    hh.MergedHeatingDemand = hh.KantonTotalEnergyDemand;
                }

                dbHouses.Save(h);
                dbHouses.Save(hh);
            }

            dbHouses.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var localnetEntries = dbRaw.Fetch<Localnet>();
            var houses = dbHouse.Fetch<House>();
            var househeatingentries = dbHouse.Fetch<HouseHeating>();
            MakeOilEnergySankey();
            MakeHeatingSystemSankey();
            MakeHeatingsystemMapLocalnet();
            MakeGasEnergySankey();
            MakeWärmeEnergySankey();
            MakeHeatingsystemMapEBBE();
            MakeEtagenHeizungenLocalnet();
            MakeEbbeLocalnetComparisonSankey();
            MakeHeatingIntensityMap();

            void MakeHeatingSystemSankey()
            {
                var ssa = new SingleSankeyArrow("Houses", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Houses total", houses.Count, 5000, Orientation.Straight));
                var housesWithZeroLocalnetHeatingSystems = 0;
                var housesWithOneLocalnetHeatingSystems = 0;
                var housesWithManyLocalnetHeatingSystems = 0;
                foreach (var house in houses) {
                    var houseHeatings = househeatingentries.Single(x => x.HouseGuid == house.HouseGuid);
                    if (houseHeatings.LocalnetHeatingSystemEntryCount == 0) {
                        housesWithZeroLocalnetHeatingSystems++;
                    }
                    else if (houseHeatings.LocalnetHeatingSystemEntryCount == 1) {
                        housesWithOneLocalnetHeatingSystems++;
                    }
                    else {
                        housesWithManyLocalnetHeatingSystems++;
                    }
                }

                ssa.AddEntry(new SankeyEntry("Häuser mit 0 Localnet heizungen", housesWithZeroLocalnetHeatingSystems * -1, 5000, Orientation.Down));
                ssa.AddEntry(new SankeyEntry("Häuser mit 1 Localnet Heizungen", housesWithOneLocalnetHeatingSystems * -1, 5000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Häuser mit mehreren Localnet Heizungen", housesWithManyLocalnetHeatingSystems * -1, 5000, Orientation.Up));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakeEbbeLocalnetComparisonSankey()
            {
                var ssa = new SingleSankeyArrow("MakeEbbeLocalnetComparisonSankey", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                double ebbeHeizungsBedarf = 0;
                double localnetHeizungsBedarf = 0;
                double mergedHeizungsBedarf = 0;
                double localnetFernWärmeBedarf = 0;
                double localnetGasBedarf = 0;
                double mergedHeizungsBedarfOnlyLocalnetHouses = 0;
                double ebbeFernwärme = 0;
                foreach (var houseHeating in househeatingentries) {
                    ebbeHeizungsBedarf += houseHeating.KantonTotalEnergyDemand;
                    localnetHeizungsBedarf += houseHeating.LocalnetCombinedEnergyDemand;
                    mergedHeizungsBedarf += houseHeating.MergedHeatingDemand;
                    localnetGasBedarf += houseHeating.LocalnetGasEnergyUse;
                    localnetFernWärmeBedarf += houseHeating.LocalnetFernwärmeEnergyUse;
                    if (houseHeating.KantonHeatingMethods.Contains(HeatingSystemType.Fernwärme)) {
                        ebbeFernwärme += houseHeating.KantonTotalEnergyDemand;
                    }

                    var house = houses.Single(x => x.HouseGuid == houseHeating.HouseGuid);
                    if (house.GebäudeObjectIDs.Count > 0) {
                        mergedHeizungsBedarfOnlyLocalnetHouses += houseHeating.MergedHeatingDemand;
                    }
                }

                var filenameCsv = MakeAndRegisterFullFilename("HeizungsVergleich.csv", Name, "", slice);
                using (var sw = new StreamWriter(filenameCsv)) {
                    sw.WriteLine("Ebbe Heizungsbedarf gesamt; " + ebbeHeizungsBedarf);
                    sw.WriteLine("Localnet Gas Bedarf;" + localnetGasBedarf);
                    sw.WriteLine("Localnet Fernwärme Bedarf;" + localnetFernWärmeBedarf);
                    sw.WriteLine("Localnet Combined Bedarf;" + localnetHeizungsBedarf);
                    sw.WriteLine("Merged Bedarf;" + mergedHeizungsBedarf);
                    sw.WriteLine("Merged Bedarf nur für Häuser mit Localnet Stromanschluss;" + mergedHeizungsBedarfOnlyLocalnetHouses);
                    sw.WriteLine("Ebbe Schätzung Fernwärme;" + ebbeFernwärme);
                }

                const double factor = 1_000_000;
                var diff = ebbeHeizungsBedarf - localnetHeizungsBedarf;
                ssa.AddEntry(new SankeyEntry("EbbeEnergieBedarf", ebbeHeizungsBedarf / factor, 500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Merged Heizungsenergiebedarf", localnetHeizungsBedarf * -1 / factor, 500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Differenz", diff * -1 / factor, 500, Orientation.Up));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeOilEnergySankey()
            {
                var ebbes = dbRaw.Fetch<EnergiebedarfsdatenBern>();
                double sumEbbeEnergyölHeizung = ebbes.Where(x => x.upd_genhz == 7201).Sum(x => x.calc_ehz);
                double sumEbbeEnergyölWw = ebbes.Where(x => x.upd_genww == 7201).Sum(x => x.calc_eww);
                var sumOilUsed = househeatingentries.Where(x => x.KantonHeatingMethods.Contains(HeatingSystemType.Öl)).Select(x => x.KantonTotalEnergyDemand).Sum();
                //double sumOilFeuerung = househeatingentries.Where(x => x.KantonHeatingMethods.Contains(HeatingSystemType.FeuerungsstättenOil)).Select(x => x.KantonTotalEnergyDemand).Sum();
                var feuerung = dbRaw.Fetch<FeuerungsStaette>();
                double sumFeuerungsstätteHaus = 0;
                var oelcount = 0;
                foreach (var staette in feuerung) {
                    if (staette.Brennstoff == "Oel") {
                        var ebbeEntry = ebbes.Where(x => x.egid == staette.EGID).ToList();
                        foreach (var bern in ebbeEntry) {
                            sumFeuerungsstätteHaus += bern.calc_ehzww;
                        }

                        oelcount++;
                    }
                }

                Log(MessageType.Info, "Total Öl Gebäude aus feuerungsstätten: " + oelcount);
                const double factor = 1_000_000;
                var ssa = new SingleSankeyArrow("Öl", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Öl Heizung", sumEbbeEnergyölHeizung / factor, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Öl WW", sumEbbeEnergyölWw / factor, 500, Orientation.Down));
                var diff = sumEbbeEnergyölHeizung + sumEbbeEnergyölWw - sumOilUsed - sumFeuerungsstätteHaus;
                ssa.AddEntry(new SankeyEntry("BFH Ebbe Öl", sumOilUsed * -1 / factor, 500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Beco Öl", sumFeuerungsstätteHaus * -1 / factor, 500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Differenz aus der Komplex-Bildung", diff / factor * -1, 500, Orientation.Straight));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeGasEnergySankey()
            {
                var verbrauchsList = localnetEntries.Where(x => x.Verrechnungstyp == "Erdgasverbrauch").ToList();
                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null) {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                var sumAllocatedGasUse = househeatingentries.Sum(x => x.LocalnetGasEnergyUse);
                var ssa = new SingleSankeyArrow("Erdgas", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Rohdaten Total", totalSum / factor, 5000, Orientation.Straight));

                ssa.AddEntry(new SankeyEntry("Heizungen zugewiesen", sumAllocatedGasUse * -1 / factor, 5000, Orientation.Down));
                ssa.AddEntry(new SankeyEntry("Keinen Heizungen zugewiesen", (totalSum - sumAllocatedGasUse) * -1 / factor, 5000, Orientation.Straight));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeWärmeEnergySankey()
            {
                var verbrauchsList = localnetEntries.Where(x => x.Verrechnungstyp == "Arbeitspreis").ToList();
                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null) {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                var sumAllocatedGasUse = househeatingentries.Sum(x => x.LocalnetFernwärmeEnergyUse);
                var ssa = new SingleSankeyArrow("Fernwärme", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Rohdaten Total", totalSum / factor, 5000, Orientation.Straight));

                ssa.AddEntry(new SankeyEntry("Heizungen zugewiesen", sumAllocatedGasUse * -1 / factor, 5000, Orientation.Down));
                ssa.AddEntry(new SankeyEntry("Keinen Heizungen zugewiesen", (totalSum - sumAllocatedGasUse) * -1 / factor, 5000, Orientation.Straight));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHeatingsystemMapLocalnet()
            {
                RGB GetColor(House h)
                {
                    var s = househeatingentries.Single(x => x.HouseGuid == h.HouseGuid);
                    if (s.LocalnetGasEnergyUse > 0) {
                        return Constants.Green;
                    }

                    return Constants.Black;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapLocalnetGasHeizungen.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("No Localnet heating", Constants.Black),
                    new MapLegendEntry("Localnet heating", Constants.Green)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }

            void MakeEtagenHeizungenLocalnet()
            {
                RGB GetColor(House h)
                {
                    var s = househeatingentries.Single(x => x.HouseGuid == h.HouseGuid);
                    if (s.LocalnetHeatingSystemEntryCount == 0) {
                        return Constants.Black;
                    }

                    if (s.LocalnetHeatingSystemEntryCount == 1) {
                        return Constants.Blue;
                    }

                    return Constants.Red;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapLocalnetHeatingSystemCountsPerHouse.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("No Localnet heating", Constants.Black),
                    new MapLegendEntry("Genau 1 Localnet Heizsystem", Constants.Blue),
                    new MapLegendEntry("Viele Localnet Heizsysteme", Constants.Red)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }

            // ReSharper disable once InconsistentNaming
            void MakeHeatingsystemMapEBBE()
            {
                RGB GetColor(House h)
                {
                    var s = househeatingentries.Single(x => x.HouseGuid == h.HouseGuid);
                    if (s.KantonHeatingMethods.Contains(HeatingSystemType.Gas)) {
                        return Constants.Green;
                    }

                    return Constants.Black;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHeatingSystemTypeEbbePerHouse.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("EBBE Gas", Constants.Green),
                    new MapLegendEntry("Sonstiges", Constants.Black)
                };
                //legendEntries.Add(new MapLegendEntry("EBBE Strom", 0, 0, 255));
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }

            // ReSharper disable once InconsistentNaming
            void MakeHeatingIntensityMap()
            {
                var maxEnergyDensity = househeatingentries.Max(x => x.MergedHeatingEnergyDensity);
                var maxEnergy = househeatingentries.Max(x => x.MergedHeatingDemand);

                RGBWithSize GetColor(House h)
                {
                    var s = househeatingentries.Single(x => x.HouseGuid == h.HouseGuid);
                    var color = 250 * s.MergedHeatingEnergyDensity / maxEnergyDensity;
                    var energy = Math.Log(s.MergedHeatingDemand / maxEnergy * 100) * 50;
                    if (energy < 10) {
                        energy = 10;
                    }

                    return new RGBWithSize((int)color, 0, 0, (int)energy);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHeatingIntensityPerHouse.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Energiedichte: 0", Constants.Black),
                    new MapLegendEntry("Energiedichte: " + maxEnergyDensity, Constants.Red)
                };
                //legendEntries.Add(new MapLegendEntry("Sonstiges", Constants.Black));
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }

        private static HeatingSystemType GetHeatingMethodString(int ebGheiz)
        {
            switch (ebGheiz) {
                case 7200: return HeatingSystemType.None;
                case 7100: return HeatingSystemType.None;
                case 7201: return HeatingSystemType.Öl;
                case 7101: return HeatingSystemType.Öl;
                case 7202: return HeatingSystemType.Kohle;
                case 7102: return HeatingSystemType.Kohle;
                case 7203: return HeatingSystemType.Gas;
                case 7103: return HeatingSystemType.Gas;
                case 7204: return HeatingSystemType.Electricity;
                case 7104: return HeatingSystemType.Electricity;
                case 7205: return HeatingSystemType.Holz;
                case 7105: return HeatingSystemType.Holz;
                case 7206: return HeatingSystemType.Heatpump;
                case 7207: return HeatingSystemType.SolarThermal;
                case 7208: return HeatingSystemType.Fernwärme;
                case 7209: return HeatingSystemType.Other;
                case 7109: return HeatingSystemType.Other;
                case 0: return HeatingSystemType.Other;
                default:
                    throw new Exception("unknown: " + ebGheiz);
            }
        }
    }
}