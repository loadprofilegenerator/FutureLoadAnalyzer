using System;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class E_AssignParkingSpaces : RunableWithBenchmark {
        public E_AssignParkingSpaces([NotNull] ServiceRepository services)
            : base(nameof(E_AssignParkingSpaces), Stage.Houses, 500, services, true, new ParkingSpaceCharts())
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<ParkingSpace>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var households = dbHouses.Fetch<Household>();
            //var houses = dbHouses.Fetch<House>();
            var cars = dbHouses.Fetch<Car>();
            dbHouses.BeginTransaction();
            foreach (var car in cars) {
                var household = households.Single(x => x.HouseholdGuid == car.HouseholdGuid);
                var ps = new ParkingSpace(car.HouseholdGuid, Guid.NewGuid().ToString(), car.HouseGuid,household.HausAnschlussGuid,household.Name + " car") {
                    CarGuid = car.CarGuid
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