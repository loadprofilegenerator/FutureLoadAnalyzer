using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class R_CheckBusinessAssignments : RunableWithBenchmark {
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public R_CheckBusinessAssignments([NotNull] ServiceRepository services) : base(nameof(R_CheckBusinessAssignments),
            Stage.Houses,
            1700,
            services,
            false)
        {
            DevelopmentStatus.Add("Make yearly gas use entries properly");
            DevelopmentStatus.Add("Make yearly fernwärme entries properly");
        }

        protected override void RunActualProcess()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            //double check
            var potentialBusinesses = dbHouse.Fetch<PotentialBusinessEntry>();
            var potentialBuildingInfrastructures = dbHouse.Fetch<PotentialBuildingInfrastructure>();
            var buildingInfrastructures = dbHouse.Fetch<BuildingInfrastructure>();
            var heatingsystems = dbHouse.Fetch<PotentialHeatingSystemEntry>();
            var businessNames = dbRaw.Fetch<BusinessName>();
            var wkw = dbHouse.Fetch<KleinWasserkraft>();
            List<string> businessMatches = new List<string>();
            foreach (var businessName in businessNames) {
                var matchingPotentials = potentialBusinesses.Where(x => x.BusinessName == businessName.Name).ToList();
                var matchingInfrastructures = potentialBuildingInfrastructures.Where(x => x.Geschäftspartner == businessName.Name).ToList();
                var matchingInfrastructures2 = buildingInfrastructures.Where(x => x.Geschäftspartner == businessName.Name).ToList();
                var matchingheating = heatingsystems.Where(x => x.Geschäftspartner == businessName.Name).ToList();
                var matchingwkw = wkw.Where(x => x.Geschäftspartner == businessName.Name).ToList();
                if (matchingPotentials.Count < 1 && matchingInfrastructures.Count == 0 && matchingheating.Count == 0 &&
                    matchingInfrastructures2.Count == 0 && matchingwkw.Count == 0) {
                    businessMatches.Add(businessName.Name);
                }
            }

            if (businessMatches.Count > 0) {
                throw new FlaException("No business found for " + string.Join("\n", businessMatches));
            }
        }
    }
}