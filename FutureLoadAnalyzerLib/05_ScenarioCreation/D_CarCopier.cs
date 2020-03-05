using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class D_CarCopier : RunableForSingleSliceWithBenchmark {
        public D_CarCopier([NotNull] ServiceRepository services) : base(nameof(D_CarCopier),
            Stage.ScenarioCreation,
            400,
            services,
            false,
            new CarCharts(services, Stage.ScenarioCreation))
        {
        }

        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var households = dbDstHouses.FetchAsRepo<Household>();
            dbDstHouses.RecreateTable<Car>();
            var srcCars = dbSrcHouses.FetchAsRepo<Car>();
            CheckCarsPerHousehold(srcCars);
            //filter cars for eliminated households
            var householdGuids = households.ToGuidHashset();
            if (srcCars.Count == 0) {
                throw new Exception("No cars were found in the scenario " + slice.DstScenario + " - " + slice.DstYear);
            }

            var yearsToAge = slice.DstYear - slice.PreviousSlice?.DstYear ?? throw new FlaException("Previous scenario was not set");
            dbDstHouses.BeginTransaction();
            WeightedRandomAllocator<Car> allocator = new WeightedRandomAllocator<Car>(Services.Rnd, Services.Logger);
            var carsToPickFrom = srcCars.Where(x => x.CarType == CarType.Gasoline).ToList();
            bool failOnOversubscribe = slice.DstYear == 2050 ? false : true;
            int numberOfTargetElectricCars = (int)(slice.TotalPercentageOfElectricCars * srcCars.Count);
            int numberOfCurrentElectricCars = srcCars.Count - carsToPickFrom.Count;
            int numberOfNewElectricCars = numberOfTargetElectricCars - numberOfCurrentElectricCars;
            if (numberOfNewElectricCars < 0) {
                throw new FlaException("Negative target electric cars in slice " + slice);
            }

            var carsToChange = allocator.PickNumberOfObjects(carsToPickFrom, x => x.Age, numberOfNewElectricCars, failOnOversubscribe);
            int carsSaved = 0;
            List<Car> carsToDelete = new List<Car>();

            foreach (var car in srcCars) {
                if (!householdGuids.Contains(car.HouseholdGuid)) {
                    var otherhouseholds = households.GetByReferenceGuidWithEmptyReturns(car.HouseGuid, "houseguid", x => x.HouseGuid);
                    if (otherhouseholds.Count == 0) {
                        carsToDelete.Add(car);
                        continue;
                    }

                    var otherCars = srcCars.GetByReferenceGuidWithEmptyReturns(car.HouseGuid, "houseguid", x => x.HouseGuid);
                    var hhswithCarGuids = otherCars.Select(x => x.HouseholdGuid).ToList();
                    var householdsWithoutCars = otherhouseholds.Where(x => !hhswithCarGuids.Contains(x.Guid)).ToList();
                    if (householdsWithoutCars.Count > 0) {
                        car.HouseholdGuid = householdsWithoutCars[Services.Rnd.Next(householdsWithoutCars.Count)].Guid;
                    }
                    else {
                        car.HouseholdGuid = otherhouseholds[Services.Rnd.Next(householdsWithoutCars.Count)].Guid;
                    }
                }
            }

            var carsToSave = srcCars.GetValueList();
            foreach (var car in carsToDelete) {
                carsToSave.Remove(car);
            }

            CheckCarsPerHousehold(carsToSave);
            int targetCarCount = (int)(households.SelectMany(x => x.Occupants).Count() * slice.CarOwnershipPercentage);
            if (carsToSave.Count > targetCarCount) {
                //clear excess cars
                int excessCars = carsToSave.Count - targetCarCount;
                var carsToSell = allocator.PickNumberOfObjects(carsToSave, x => x.Age, excessCars, false);
                foreach (var sellcar in carsToSell) {
                    carsToSave.Remove(sellcar);
                }
            }
            else {
                int carsToCreate = targetCarCount - carsToSave.Count;
                while (carsToCreate > 0) {
                    var dict = CheckCarsPerHousehold(carsToSave);
                    var householdGuid = dict.First().Key;
                    if (dict.First().Value > 2) {
                        throw new FlaException("more than 2 cars per household");
                    }

                    string houseguid = households.GetByGuid(householdGuid).HouseGuid;
                    CarType targetCarType = CarType.Gasoline;
                    if (carsToSave.Count(x => x.CarType == CarType.Electric) < (slice.TotalPercentageOfElectricCars * carsToSave.Count)) {
                        targetCarType = CarType.Electric;
                    }

                    Car newcar = new Car(householdGuid, Guid.NewGuid().ToString(), 0, targetCarType, houseguid);
                    carsToCreate--;
                    carsToSave.Add(newcar);
                }
            }

            CheckCarsPerHousehold(carsToSave);
            foreach (var car in carsToSave) {
                car.ID = 0;
                car.Age += yearsToAge;
                if (carsToChange.Contains(car)) {
                    car.CarType = CarType.Electric;
                    car.Age = 0;
                }

                carsSaved++;
                dbDstHouses.Save(car);
            }

            //make new Cars
            Info("Converted " + carsToChange.Count + " cars, out of a total of " + carsSaved);
            //convert cars to electric
            dbDstHouses.CompleteTransaction();
        }

        [NotNull]
        private static Dictionary<string, int> CheckCarsPerHousehold([NotNull] [ItemNotNull] IEnumerable<Car> srcCars)
        {
            Dictionary<string, int> carsPerHousehold = new Dictionary<string, int>();
            foreach (var car in srcCars) {
                if (!carsPerHousehold.ContainsKey(car.HouseholdGuid)) {
                    carsPerHousehold.Add(car.HouseholdGuid, 0);
                }

                carsPerHousehold[car.HouseholdGuid]++;
                if (carsPerHousehold[car.HouseholdGuid] > 3) {
                    throw new FlaException("too many cars per household");
                }
            }

            var l = carsPerHousehold.OrderBy(key => key.Value);
            var dict = l.ToDictionary(keyItem => keyItem.Key, valueItem => valueItem.Value);
            return dict;
        }
    }
}