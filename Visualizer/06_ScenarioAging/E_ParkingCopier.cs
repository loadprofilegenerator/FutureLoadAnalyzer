using System;
using System.Collections.Generic;
using System.Diagnostics;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class E_ParkingCopier : RunableForSingleSliceWithBenchmark {
        public E_ParkingCopier([NotNull] ServiceRepository services)
            : base(nameof(E_ParkingCopier), Stage.ScenarioCreation, 500, services, false, new ParkingSpaceCharts())
        {
            DevelopmentStatus.Add("Fix this");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<ParkingSpace>(Stage.Houses, parameters);
            Debug.Assert(parameters.PreviousScenario != null, "parameters.PreviousScenario != null");
            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenario).Database;
            var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var srcCars = dbSrcHouses.Fetch<Car>();
            var srcParking = dbSrcHouses.Fetch<ParkingSpace>();
            if (srcCars.Count == 0) {
                throw new Exception("No cars were found");
            }

            dbDstHouses.BeginTransaction();
            var carByGuid = new Dictionary<string, Car>();
            foreach (var car in srcCars) {
                carByGuid.Add(car.CarGuid, car);
            }

            foreach (var parkingSpace in srcParking) {
                parkingSpace.ParkingSpaceID = 0;
                if (carByGuid.ContainsKey(parkingSpace.CarGuid ?? throw new InvalidOperationException())) {
                    var car = carByGuid[parkingSpace.CarGuid];
                    if (car.CarType == CarType.Electric && parkingSpace.ChargingStationType != ChargingStationType.NoCharging) {
                        parkingSpace.ChargingStationType = ChargingStationType.ThreekW;
                    }
                    dbDstHouses.Save(parkingSpace);
                }
            }

            //make new Cars
            //convert cars to electric
            dbDstHouses.CompleteTransaction();
#pragma warning restore 162
        }
    }
}