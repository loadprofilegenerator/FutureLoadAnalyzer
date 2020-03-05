using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.Sankey;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class L_Airconditioning : RunableForSingleSliceWithBenchmark {
        public L_Airconditioning([NotNull] ServiceRepository services) : base(nameof(L_Airconditioning),
            Stage.ScenarioCreation,
            1200,
            services,
            false)
        {
            DevelopmentStatus.Add("Not implemented");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<AirConditioningEntry>();
            var srcAirconditioningEntries = dbSrcHouses.Fetch<AirConditioningEntry>();
            var srcHouses = dbSrcHouses.Fetch<House>();
            var airConGuids = srcAirconditioningEntries.Select(x => x.HouseGuid).ToList();
            var hausanschlusess = dbSrcHouses.Fetch<Hausanschluss>();
            var businesses = dbDstHouses.Fetch<BusinessEntry>();
            var housesWithoutAirconditioning1 = srcHouses.Where(x => !airConGuids.Contains(x.Guid)).ToList();
            if (housesWithoutAirconditioning1.Count == 0) {
                throw new FlaException("no houses left without airconditioning");
            }

            foreach (var conditioningEntry in srcAirconditioningEntries) {
                conditioningEntry.HausAnschlussGuid.Should().NotBeNullOrWhiteSpace();
                var ha = hausanschlusess.First(x => x.Guid == conditioningEntry.HausAnschlussGuid);
                if (ha.ObjectID.ToLower().Contains("kleinanschluss")) {
                    throw new FlaException("No ac at kleinanschluss");
                }
            }

            // ReSharper disable once ArrangeLocalFunctionBody
            double CalculateYearlyAirConditioningEnergyConsumption(House h) =>
                h.EnergieBezugsFläche * slice.AirConditioningEnergyIntensityInKWhPerSquareMeter;

            double totalEbf = srcHouses.Sum(x => x.EnergieBezugsFläche);
            double totalTargetAirConditioning =
                totalEbf * slice.PercentageOfAreaWithAirConditioning * slice.AirConditioningEnergyIntensityInKWhPerSquareMeter;
            double currentAirConditioning = srcAirconditioningEntries.Sum(x => x.EffectiveEnergyDemand);
            double newlyInstealledEnergyForAirconditioning = totalTargetAirConditioning - currentAirConditioning;
            if (newlyInstealledEnergyForAirconditioning < 0) {
                throw new FlaException("Negative air conditioning target. Currently installed in " + slice + " are " +
                                       currentAirConditioning / (totalEbf * slice.AirConditioningEnergyIntensityInKWhPerSquareMeter));
            }

            bool failOnOver = slice.DstYear != 2050;
            WeightedRandomAllocator<House> allocator = new WeightedRandomAllocator<House>(Services.Rnd, Services.Logger);
            var housesForAirConditioning = allocator.PickObjectUntilLimit(housesWithoutAirconditioning1,
                x => x.EnergieBezugsFläche,
                CalculateYearlyAirConditioningEnergyConsumption,
                newlyInstealledEnergyForAirconditioning,
                failOnOver);
            var dstAirConditioningEntries = new List<AirConditioningEntry>();
            foreach (var house in housesForAirConditioning) {
                var businessesInHouse = businesses.Where(x => x.HouseGuid == house.Guid).ToList();
                AirConditioningType act = AirConditioningType.Commercial;
                if (businessesInHouse.Count == 0) {
                    act = AirConditioningType.Residential;
                }

                foreach (var entry in businessesInHouse) {
                    if (entry.BusinessType == BusinessType.Industrie) {
                        act = AirConditioningType.Industrial;
                    }
                }

                Hausanschluss ha = house.GetHausanschlussByIsn(new List<int>(), null, hausanschlusess, MyLogger, false);
                if (ha != null && ha.ObjectID.ToLower().Contains("kleinanschluss")) {
                    ha = null;
                }

                if (ha != null) {
                    var ace = new AirConditioningEntry(house.Guid,
                        Guid.NewGuid().ToString(),
                        CalculateYearlyAirConditioningEnergyConsumption(house),
                        1,
                        act,
                        ha.Guid,
                        house.ComplexName + " - Air Conditioning",
                        "");
                    dstAirConditioningEntries.Add(ace);
                }
            }

            foreach (var airconditioningEntry in srcAirconditioningEntries) {
                airconditioningEntry.ID = 0;
                dstAirConditioningEntries.Add(airconditioningEntry);
            }

            //final checks
            foreach (var dstAirConditioningEntry in dstAirConditioningEntries) {
                dstAirConditioningEntry.HausAnschlussGuid.Should().NotBeNullOrWhiteSpace();
                var ha = hausanschlusess.First(x => x.Guid == dstAirConditioningEntry.HausAnschlussGuid);
                if (ha.ObjectID.ToLower().Contains("kleinanschluss")) {
                    throw new FlaException("No ac at kleinanschluss");
                }
            }

            dbDstHouses.BeginTransaction();
            foreach (var airConditioningEntry in dstAirConditioningEntries) {
                dbDstHouses.Save(airConditioningEntry);
            }

            dbDstHouses.CompleteTransaction();
        }

        protected override void RunChartMaking(ScenarioSliceParameters slice)
        {
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var airConditioningEntries = dbDstHouses.Fetch<AirConditioningEntry>();
            var houses = dbDstHouses.Fetch<House>();
            MakeAcSystemEnergySankey();
            MakeAcSystemCountSankey();

            void MakeAcSystemEnergySankey()
            {
                var ssa1 = new SingleSankeyArrow("HouseAirConditioningEnergy", 60, MyStage, SequenceNumber, Name, slice, Services);

                double potentialForAirConditioning =
                    houses.Sum(x => x.EnergieBezugsFläche * slice.AirConditioningEnergyIntensityInKWhPerSquareMeter) / Constants.GWhFactor;
                double actualAirConditioningEnergy = airConditioningEntries.Sum(x => x.EffectiveEnergyDemand) / Constants.GWhFactor;
                ssa1.AddEntry(new SankeyEntry("Houses", potentialForAirConditioning, 60, Orientation.Straight));

                ssa1.AddEntry(new SankeyEntry("Houses without Airconditioning",
                    (potentialForAirConditioning - actualAirConditioningEnergy) * -1,
                    60,
                    Orientation.Up));
                ssa1.AddEntry(new SankeyEntry("Houses with Airconditioning", actualAirConditioningEnergy * -1, 60, Orientation.Down));

                Services.PlotMaker.MakeSankeyChart(ssa1);
            }

            void MakeAcSystemCountSankey()
            {
                var ssa1 = new SingleSankeyArrow("HouseAirConditioningCount", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa1.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var airConditioningHouseGuids = airConditioningEntries.Select(x => x.HouseGuid).Distinct().ToList();
                var housesWithoutAirconditioing = houses.Where(x => !airConditioningHouseGuids.Contains(x.Guid)).Distinct().ToList();

                ssa1.AddEntry(new SankeyEntry("Houses without Airconditioning", housesWithoutAirconditioing.Count * -1, 5000, Orientation.Up));
                ssa1.AddEntry(new SankeyEntry("Houses with Airconditioning", airConditioningHouseGuids.Count * -1, 5000, Orientation.Down));

                Services.PlotMaker.MakeSankeyChart(ssa1);
            }
        }
    }
}