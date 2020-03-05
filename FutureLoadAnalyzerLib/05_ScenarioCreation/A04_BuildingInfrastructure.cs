using System;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class A04_BuildingInfrastructure : RunableForSingleSliceWithBenchmark {
        public A04_BuildingInfrastructure([NotNull] ServiceRepository services) : base(nameof(A04_BuildingInfrastructure),
            Stage.ScenarioCreation,
            104,
            services,
            false)
        {
            //todo: include factors for adjusting the energy consumption into the parameters
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<BuildingInfrastructure>();
            var srcBuildingInfrastructures = dbSrcHouses.Fetch<BuildingInfrastructure>();
            if (srcBuildingInfrastructures.Count == 0) {
                throw new Exception("No building infrastructures were found");
            }

            dbDstHouses.BeginTransaction();
            foreach (var buildingInfrastructure in srcBuildingInfrastructures) {
                buildingInfrastructure.ID = 0;
                double energy = buildingInfrastructure.LocalnetLowVoltageYearlyTotalElectricityUse +
                                buildingInfrastructure.LocalnetHighVoltageYearlyTotalElectricityUse;

                double energyReduction = energy * (1 - slice.EnergyReductionFactorBuildingInfrastructure);
                if (Math.Abs(energyReduction) > 0.000001) {
                    buildingInfrastructure.SetEnergyReduction(slice.ToString(), energyReduction);
                    //throw new FlaException("Factor 0 for house infrastructure energy reduction in slice " + slice);
                }

                dbDstHouses.Save(buildingInfrastructure);
            }

            dbDstHouses.CompleteTransaction();
        }
    }
}