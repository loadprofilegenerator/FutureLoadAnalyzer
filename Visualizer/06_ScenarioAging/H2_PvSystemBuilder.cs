using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class H2_PvSystemBuilder : RunableForSingleSliceWithBenchmark {
        public H2_PvSystemBuilder([NotNull] ServiceRepository services)
            : base(nameof(H2_PvSystemBuilder), Stage.ScenarioCreation,
                800, services, false, new PVInstalledCharts())
        {
            DevelopmentStatus.Add("//todo: do the power calculation better");
            DevelopmentStatus.Add("Visualisation: Potential vs currently installed power" );
            DevelopmentStatus.Add("proper alignment and stuff");
        }

        private class RangeEntry {
            public RangeEntry([NotNull] House house, double weight, double startRange, double endRange)
            {
                House = house;
                Weight = weight;
                StartRange = startRange;
                EndRange = endRange;
            }

            public double Weight { [UsedImplicitly] get; }
            public double StartRange { get; }
            public double EndRange { get; }

            [NotNull]
            public House House { get; }
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<PvSystemEntry>(Stage.Houses, parameters);
            Services.SqlConnection.RecreateTable<PVPotential>(Stage.Houses, parameters);
            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenarioNotNull).Database;
            var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var srcHouses = dbSrcHouses.Fetch<House>();
            var srcPVPotentials = dbSrcHouses.Fetch<PVPotential>();
            var srcPvSystemEntries = dbSrcHouses.Fetch<PvSystemEntry>();
            dbDstHouses.BeginTransaction();
            foreach (var potential in srcPVPotentials) {
                potential.ID = 0;
                dbDstHouses.Save(potential);
            }

            //copy pv systems from previous slice
            HashSet<string> houseGuidsForSystemsWithPV  = new  HashSet<string>();
            Dictionary<string, double> pvPotentialByHouseGuid = new Dictionary<string, double>();
            foreach (var pvpot in srcPVPotentials) {
                if (!houseGuidsForSystemsWithPV.Contains(pvpot.HouseGuid)) {
                    houseGuidsForSystemsWithPV.Add(pvpot.HouseGuid);
                    pvPotentialByHouseGuid.Add(pvpot.HouseGuid,0);
                }
                pvPotentialByHouseGuid[pvpot.HouseGuid] += pvpot.SonnendachStromErtrag;
            }
            var potentialhousesForPvSystems = srcHouses.Where(x => houseGuidsForSystemsWithPV.Contains(x.HouseGuid)).ToList();
            foreach (var entry in srcPvSystemEntries) {
                var toremove = potentialhousesForPvSystems.FirstOrDefault(x => x.HouseGuid == entry.HouseGuid);
                if (toremove != null) {
                    potentialhousesForPvSystems.Remove(toremove);
                }
                if (entry.PVAreas.Count == 0)
                {
                    throw new FlaException("No PV System areas defined.");
                }
                entry.Pvid = 0;
                dbDstHouses.Save(entry);
            }

            var pvToInstallInkWh = parameters.PvPowerToInstallInGwh*1_000_000;
            while (pvToInstallInkWh > 0) {
                //make ranges
                var rangeEntries = SetRanges(potentialhousesForPvSystems, pvPotentialByHouseGuid);
                //randomly pick
                var max = rangeEntries.Max(x => x.EndRange);
                var pick = Services.Rnd.NextDouble() * max;
                var rangeEntry = rangeEntries.Single(x => pick >= x.StartRange && pick <= x.EndRange);
                //remove house
                potentialhousesForPvSystems.Remove(rangeEntry.House);
                //save pvsystementry
                var pvPotenial = pvPotentialByHouseGuid[rangeEntry.House.HouseGuid];
                var hausanschlsusGuid = rangeEntry.House.Hausanschluss[0].HausanschlussGuid;

                var pvSystemEntry = new PvSystemEntry(rangeEntry.House.HouseGuid, Guid.NewGuid().ToString(),hausanschlsusGuid,rangeEntry.House.ComplexName) {
                    YearlyPotential =pvPotenial
                };
                var areas = srcPVPotentials.Where(x => x.HouseGuid == rangeEntry.House.HouseGuid).ToList();
                foreach (var area in areas)
                {
                    pvSystemEntry.PVAreas.Add(new PVSystemArea(area.Ausrichtung, area.Neigung, area.SonnendachStromErtrag));
                }

                if (pvSystemEntry.PVAreas.Count == 0) {
                    throw new FlaException("No PV System areas defined.");
                }
                pvToInstallInkWh -= pvSystemEntry.YearlyPotential;
                pvSystemEntry.BuildYear = parameters.DstYear;
                int dstIdx = Services.Rnd.Next(rangeEntry.House.Hausanschluss.Count);
                pvSystemEntry.HausAnschlussGuid = rangeEntry.House.Hausanschluss[dstIdx].HausanschlussGuid;
                dbDstHouses.Save(pvSystemEntry);
                //deduct from pvtoinstall
            }

            dbDstHouses.CompleteTransaction();
        }

        [ItemNotNull]
        [NotNull]
        private static List<RangeEntry> SetRanges([ItemNotNull] [NotNull] List<House> houses, [NotNull] Dictionary<string,double> pvPotentialByHouse)
        {
            var rangeEntries = new List<RangeEntry>();
            double currentRangeValue = 0;
            foreach (var house in houses) {
                var weight = pvPotentialByHouse[house.HouseGuid];
                var entry = new RangeEntry(house, weight, currentRangeValue, currentRangeValue + weight);
                currentRangeValue += weight;
                rangeEntries.Add(entry);
            }

            return rangeEntries;
        }
    }
}