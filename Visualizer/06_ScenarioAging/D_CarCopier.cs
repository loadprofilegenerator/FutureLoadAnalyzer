using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class D_CarCopier : RunableForSingleSliceWithBenchmark {
        public D_CarCopier([NotNull] ServiceRepository services)
            : base(nameof(D_CarCopier), Stage.ScenarioCreation, 400, services, false, new CarCharts())
        {
            DevelopmentStatus.Add("Fix this");
        }

        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<Car>(Stage.Houses, parameters);
            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenarioNotNull).Database;
            var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var srcCars = dbSrcHouses.Fetch<Car>();
            if (srcCars.Count == 0) {
                throw new Exception("No cars were found in the scenario " + parameters.DstScenario + " - " + parameters.DstYear);
            }
            var yearsToAge = parameters.DstYear - parameters.PreviousScenario?.DstYear??throw new FlaException("Previous scenario was not set");
            dbDstHouses.BeginTransaction();
            var carsToConvert = new HashSet<string>();
            var carsToPickFrom = srcCars.Where(x => x.CarType == CarType.Gasoline).ToList();
            Log(MessageType.Info, "Found " + srcCars.Count + " cars in total, " + carsToPickFrom.Count + " are gasoline." + " Planned conversions are " + parameters.NumberOfNewElectricCars);
            for (var i = 0; i < parameters.NumberOfNewElectricCars; i++) {
                if (carsToPickFrom.Count == 0) {
                    throw new Exception("Trying to convert " + parameters.NumberOfNewElectricCars + " cars, but ran out of cars by: " + i);
                }

                var idx = Services.Rnd.Next(carsToPickFrom.Count);
                var c = carsToPickFrom[idx];
                carsToConvert.Add(c.CarGuid);
                carsToPickFrom.Remove(c);
            }

            int carsSaved = 0;
            foreach (var car in srcCars) {
                car.CarID = 0;
                car.Age += yearsToAge;
                if (carsToConvert.Contains(car.CarGuid)) {
                    car.CarType = CarType.Electric;
                    car.Age = 0;
                }

                if (car.Age < 15) {
                    carsSaved++;
                    dbDstHouses.Save(car);
                }
            }

            //make new Cars
            Log(MessageType.Info, "Converted " + carsToConvert.Count + " cars, out of a total of " + carsSaved);
            //convert cars to electric
            dbDstHouses.CompleteTransaction();
#pragma warning restore 162
        }


    }
}