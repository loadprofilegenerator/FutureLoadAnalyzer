using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class H2_PvSystemBuilder : RunableForSingleSliceWithBenchmark {
        public H2_PvSystemBuilder([NotNull] ServiceRepository services) : base(nameof(H2_PvSystemBuilder),
            Stage.ScenarioCreation,
            800,
            services,
            false,
            new PVInstalledCharts(services, Stage.ScenarioCreation))
        {
            DevelopmentStatus.Add("//todo: do the power calculation better");
            DevelopmentStatus.Add("Visualisation: Potential vs currently installed power");
            DevelopmentStatus.Add("proper alignment and stuff");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<PvSystemEntry>();
            dbDstHouses.RecreateTable<PVPotential>();
            var srcHouses = dbSrcHouses.Fetch<House>();
            var srcPVPotentials = dbSrcHouses.Fetch<PVPotential>();
            var srcPvSystemEntries = dbSrcHouses.Fetch<PvSystemEntry>();
            var hausanschlusses = dbDstHouses.Fetch<Hausanschluss>();
            dbDstHouses.BeginTransaction();
            foreach (var potential in srcPVPotentials) {
                potential.ID = 0;
                dbDstHouses.Save(potential);
            }

            //copy pv systems from previous slice
            HashSet<string> houseGuidsForSystemsWithPV = new HashSet<string>();
            Dictionary<string, double> pvPotentialByHouseGuid = new Dictionary<string, double>();
            foreach (var pvpot in srcPVPotentials) {
                if (!houseGuidsForSystemsWithPV.Contains(pvpot.HouseGuid)) {
                    houseGuidsForSystemsWithPV.Add(pvpot.HouseGuid);
                    pvPotentialByHouseGuid.Add(pvpot.HouseGuid, 0);
                }

                pvPotentialByHouseGuid[pvpot.HouseGuid] += pvpot.SonnendachStromErtrag;
            }

            var potentialhousesForPvSystems = srcHouses.Where(x => houseGuidsForSystemsWithPV.Contains(x.Guid)).ToList();
            foreach (var entry in srcPvSystemEntries) {
                var toremove = potentialhousesForPvSystems.FirstOrDefault(x => x.Guid == entry.HouseGuid);
                if (toremove != null) {
                    potentialhousesForPvSystems.Remove(toremove);
                }

                if (entry.PVAreas.Count == 0) {
                    throw new FlaException("No PV System areas defined.");
                }

                entry.ID = 0;
                dbDstHouses.Save(entry);
            }

            var pvToInstallInkWh = slice.PvPowerToInstallInGwh * 1_000_000;
            bool continueAllocation = true;
            int pvSystemCount = 0;
            while (pvToInstallInkWh > 0 && continueAllocation) {
                //make ranges
                var rangeEntries = SetRanges(potentialhousesForPvSystems, pvPotentialByHouseGuid);
                if (rangeEntries.Count == 0) {
                    continueAllocation = false;
                    continue;
                }

                //randomly pick
                var max = rangeEntries.Max(x => x.EndRange);
                var pick = Services.Rnd.NextDouble() * max;
                var rangeEntry = rangeEntries.Single(x => pick >= x.StartRange && pick <= x.EndRange);
                //remove house
                potentialhousesForPvSystems.Remove(rangeEntry.House);
                //save pvsystementry
                var pvPotenial = pvPotentialByHouseGuid[rangeEntry.House.Guid];
                pvSystemCount++;
                string erzeugerid = "PV-" + slice.DstYear + "-" + pvSystemCount;
                var hausanschlsus = rangeEntry.House.GetHausanschlussByIsn(new List<int>(), erzeugerid, hausanschlusses, MyLogger) ??
                                    throw new FlaException("no hausanschluss");
                if (hausanschlsus.ObjectID.ToLower().Contains("leuchte")) {
                    throw new FlaException("pv an leuchte in " + slice.DstYear + " " + hausanschlsus.ObjectID);
                }

                var pvSystemEntry = new PvSystemEntry(rangeEntry.House.Guid,
                    Guid.NewGuid().ToString(),
                    hausanschlsus.Guid,
                    rangeEntry.House.ComplexName,
                    erzeugerid,
                    slice.DstYear) {
                    EffectiveEnergyDemand = pvPotenial
                };
                var areas = srcPVPotentials.Where(x => x.HouseGuid == rangeEntry.House.Guid).ToList();
                foreach (var area in areas) {
                    pvSystemEntry.PVAreas.Add(new PVSystemArea(area.Ausrichtung, area.Neigung, area.SonnendachStromErtrag));
                }

                if (pvSystemEntry.PVAreas.Count == 0) {
                    throw new FlaException("No PV System areas defined.");
                }

                pvToInstallInkWh -= pvSystemEntry.EffectiveEnergyDemand;
                pvSystemEntry.BuildYear = slice.DstYear;
                dbDstHouses.Save(pvSystemEntry);
                //deduct from pvtoinstall
            }

            dbDstHouses.CompleteTransaction();
            var newPVs = dbDstHouses.FetchAsRepo<PvSystemEntry>();
            RowCollection rc = new RowCollection("pv", "pv");
            foreach (var pv in newPVs) {
                foreach (var area in pv.PVAreas) {
                    RowBuilder rb = RowBuilder.Start("HA", pv.HausAnschlussGuid);
                    rc.Add(rb);
                    rb.Add("Azimut", area.Azimut);
                    rb.Add("Tilt", area.Tilt);
                    rb.Add("Energy", area.Energy);
                }
            }

            var fn = MakeAndRegisterFullFilename("PVExport.xlsx", slice);
            XlsxDumper.WriteToXlsx(fn, rc);
        }

        [ItemNotNull]
        [NotNull]
        private static List<RangeEntry> SetRanges([ItemNotNull] [NotNull] List<House> houses, [NotNull] Dictionary<string, double> pvPotentialByHouse)
        {
            var rangeEntries = new List<RangeEntry>();
            double currentRangeValue = 0;
            foreach (var house in houses) {
                var weight = pvPotentialByHouse[house.Guid];
                var entry = new RangeEntry(house, weight, currentRangeValue, currentRangeValue + weight);
                currentRangeValue += weight;
                rangeEntries.Add(entry);
            }

            return rangeEntries;
        }

        private class RangeEntry {
            public RangeEntry([NotNull] House house, double weight, double startRange, double endRange)
            {
                House = house;
                Weight = weight;
                StartRange = startRange;
                EndRange = endRange;
            }

            public double EndRange { get; }

            [NotNull]
            public House House { get; }

            public double StartRange { get; }

            public double Weight { [UsedImplicitly] get; }
        }
    }
}