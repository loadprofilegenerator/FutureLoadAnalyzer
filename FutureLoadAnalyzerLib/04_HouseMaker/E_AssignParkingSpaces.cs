using System;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class E_AssignParkingSpaces : RunableWithBenchmark {
        public E_AssignParkingSpaces([NotNull] ServiceRepository services) : base(nameof(E_AssignParkingSpaces),
            Stage.Houses,
            500,
            services,
            true,
            new ParkingSpaceCharts(services, Stage.Houses))
        {
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<ParkingSpace>();
            var households = dbHouses.Fetch<Household>();
            var cars = dbHouses.Fetch<Car>();
            dbHouses.BeginTransaction();
            foreach (var car in cars) {
                var household = households.Single(x => x.Guid == car.HouseholdGuid);
                var ps = new ParkingSpace(car.HouseholdGuid,
                    Guid.NewGuid().ToString(),
                    car.HouseGuid,
                    household.HausAnschlussGuid,
                    household.Name + " car") {
                    CarGuid = car.Guid
                };
                if (car.CarType == CarType.Electric) {
                    ps.ChargingStationType = ChargingStationType.ThreekW;
                }
                else {
                    ps.ChargingStationType = ChargingStationType.NoCharging;
                }

                dbHouses.Save(ps);
            }

            dbHouses.CompleteTransaction();
        }
    }
}