using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A11_BuildingInfrastructureMaker : RunableWithBenchmark {
        //var fuzzyCalculator = new HouseMemberFuzzyCalc();
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public A11_BuildingInfrastructureMaker([NotNull] ServiceRepository services) : base(nameof(A11_BuildingInfrastructureMaker),
            Stage.Houses,
            11,
            services,
            false)
        {
            DevelopmentStatus.Add("Make yearly gas use entries properly");
            DevelopmentStatus.Add("Make yearly fernwärme entries properly");
        }

        protected override void RunActualProcess()
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouse.RecreateTable<BuildingInfrastructure>();
            //load data
            var potentialBuildingInfrastructures = dbHouse.Fetch<PotentialBuildingInfrastructure>();
            var houses = dbHouse.Fetch<House>();
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();
            dbHouse.BeginTransaction();
            int count = 0;
            double totalEnergy = 0;
            foreach (var pb in potentialBuildingInfrastructures) {
                House house = houses.Single(x => x.Guid == pb.HouseGuid);
                Hausanschluss ha = house.GetHausanschlussByIsn(pb.Isns, pb.Standort, hausanschlusses, Services.Logger) ??
                                   throw new FlaException("no hausanschluss");
                if (pb.Isns.Count == 0) {
                    throw new FlaException("Not a single isn");
                }

                var bi = new BuildingInfrastructure(pb.Geschäftspartner,
                    pb.LowVoltageTotalElectricityDemand,
                    pb.HighVoltageTotalElectricityDemand,
                    pb.Isns,
                    pb.Isns[0],
                    pb.HouseGuid,
                    ha.Guid,
                    pb.Guid,
                    pb.Standort,
                    pb.Geschäftspartner);
                count++;
                totalEnergy += bi.EffectiveEnergyDemand;
                dbHouse.Save(bi);
            }

            dbHouse.CompleteTransaction();
            Info("Infrastructure: " + count);
            Info("Energy: " + totalEnergy.ToString("N1"));
        }
    }
}