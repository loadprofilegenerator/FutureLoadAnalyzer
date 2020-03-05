using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B03_ComparisonProfilesToHouseData : RunableForSingleSliceWithBenchmark {
        public B03_ComparisonProfilesToHouseData([NotNull] ServiceRepository services) : base(nameof(B03_ComparisonProfilesToHouseData),
            Stage.ProfileAnalysis,
            203,
            services,
            false)
        {
            DevelopmentStatus.Add("Change to use archive entries");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            if (!Services.RunningConfig.MakeHouseSums) {
                return;
            }

            var presentSums = GetEnergySumPerHouseForThePresent();
            var energySums = GetPlannedEnergySumPerHouseForScenario(slice);
            var energyInProfiles = GetEnergyInProfiles(slice);

            RowCollection rc = new RowCollection("Comparison", "Comparison");
            rc.ColumnsToSum.Add("Summe Gegenwart");
            rc.ColumnsToSum.Add("Summe Haus Collection");
            rc.ColumnsToSum.Add("Summe Profile");
            rc.SumDivisionFactor = 1_000_000;
            presentSums.Sort((x, y) => y.Energy.CompareTo(x.Energy));
            foreach (var presentSum in presentSums) {
                RowBuilder rb = RowBuilder.Start("Hausname", presentSum.HouseName);
                rb.Add("Summe Gegenwart", presentSum.Energy);
                rb.Add("Summe Haus Collection", energySums.Single(x => x.HouseName == presentSum.HouseName).Energy);
                var profile = energyInProfiles.FirstOrDefault(x => x.HouseName == presentSum.HouseName);
                if (profile != null) {
                    rb.Add("Summe Profile", profile.Energy);
                }

                rc.Add(rb);
            }

            var fn = MakeAndRegisterFullFilename("EnergyComparison.xlsx", slice);
            XlsxDumper.WriteToXlsx(fn, rc);
            SaveToArchiveDirectory(fn, RelativeDirectory.Report, slice);
        }

        [NotNull]
        [ItemNotNull]
        private List<HouseEnergyValue> GetEnergyInProfiles([NotNull] ScenarioSliceParameters parameters)
        {
            var dbArchive =
                Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, parameters, DatabaseCode.SummedLoadForAnalysis);
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedHouseProfiles, Services.Logger);
            var results = new List<HouseEnergyValue>();
            foreach (var house in saHouses.ReadEntireTableDBAsEnumerable()) {
                if (house.Key.GenerationOrLoad == GenerationOrLoad.Load) {
                    double energySum = house.Profile.EnergySum();
                    results.Add(new HouseEnergyValue(house.Name, energySum));
                }
            }

            return results;
        }

        [NotNull]
        [ItemNotNull]
        private List<HouseEnergyValue> GetEnergySumPerHouseForThePresent()
        {
            var dbHousesPresent = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houseEnergyUse = dbHousesPresent.Fetch<HouseSummedLocalnetEnergyUse>();
            var houses = dbHousesPresent.Fetch<House>();
            var houseDict = houses.ToDictionary(x => x.Guid, y => y.ComplexName);
            var results = new List<HouseEnergyValue>();
            foreach (HouseSummedLocalnetEnergyUse energyUse in houseEnergyUse) {
                results.Add(new HouseEnergyValue(houseDict[energyUse.HouseGuid], energyUse.ElectricityUse));
            }

            return results;
        }

        [NotNull]
        [ItemNotNull]
        private List<HouseEnergyValue> GetPlannedEnergySumPerHouseForScenario([NotNull] ScenarioSliceParameters parameters)
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, parameters);

            var houses = dbHouses.Fetch<House>();
            HouseComponentRepository hcr = new HouseComponentRepository(dbHouses);

            var results = new List<HouseEnergyValue>();
            foreach (var house in houses) {
                var components = house.CollectHouseComponents(hcr);
                double energySum = 0;
                foreach (var component in components) {
                    energySum += component.EffectiveEnergyDemand;
                }

                results.Add(new HouseEnergyValue(house.ComplexName, energySum));
            }

            return results;
        }

        private class HouseEnergyValue {
            public HouseEnergyValue([NotNull] string houseName, double energy)
            {
                HouseName = houseName;
                Energy = energy;
            }

            public double Energy { get; set; }

            [NotNull]
            public string HouseName { get; set; }
        }
    }
}