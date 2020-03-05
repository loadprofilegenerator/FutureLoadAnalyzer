using System;
using System.Collections.Generic;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class E_ParkingCopier : RunableForSingleSliceWithBenchmark {
        public E_ParkingCopier([NotNull] ServiceRepository services)
            : base(nameof(E_ParkingCopier), Stage.ScenarioCreation, 500,
                services, false, new ParkingSpaceCharts(services,Stage.ScenarioCreation))
        {
            DevelopmentStatus.Add("Fix this");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<ParkingSpace>();
            var srcCars = dbSrcHouses.Fetch<Car>();
            var srcParking = dbSrcHouses.Fetch<ParkingSpace>();
            if (srcCars.Count == 0) {
                throw new Exception("No cars were found");
            }

            dbDstHouses.BeginTransaction();
            var carByGuid = new Dictionary<string, Car>();
            foreach (var car in srcCars) {
                carByGuid.Add(car.Guid, car);
            }

            foreach (var parkingSpace in srcParking) {
                parkingSpace.ID = 0;
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