using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class D_AssignCars : RunableWithBenchmark {
        public enum Direction {
            Under,
            Unknown,
            Over
        }

        public D_AssignCars([NotNull] ServiceRepository services) : base(nameof(D_AssignCars),
            Stage.Houses,
            400,
            services,
            true,
            new CarCharts(services, Stage.Houses))
        {
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var dbHousesPersistence =
                Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice, DatabaseCode.Persistence);
            dbHouses.RecreateTable<Car>();
            dbHousesPersistence.CreateTableIfNotExists<PersistentCar>();
            var households = dbHouses.Fetch<Household>();
            var allHouses = dbHouses.Fetch<House>();
            var occupants = households.SelectMany(x => x.Occupants).ToList().AsReadOnly();
            var persistentCars = dbHousesPersistence.Fetch<PersistentCar>();
            var totalPeople = occupants.Count;
            var totalStatisticalNumberOfCars = (int)(totalPeople * 0.44);
            if (occupants.Count < 16000) {
                throw new Exception("Too few people found!");
            }

            Info("Found " + persistentCars.Count + " persistent cars, planned cars: " + totalStatisticalNumberOfCars);
            var houses = allHouses.ToList();
            dbHouses.BeginTransaction();
            dbHousesPersistence.BeginTransaction();
            foreach (var persistentCar in persistentCars) {
                var house = houses.FirstOrDefault(x => x.ComplexName == persistentCar.HouseName);
                if (house == null) {
                    dbHousesPersistence.Delete(persistentCar);
                    continue;
                }

                var household = households.FirstOrDefault(x => x.HouseholdKey == persistentCar.HouseholdKey);
                if (household == null) {
                    dbHousesPersistence.Delete(persistentCar);
                    continue;
                }

                Car car = new Car(household.Guid, Guid.NewGuid().ToString(), persistentCar.Age, persistentCar.CarType, house.Guid);
                if (persistentCar.CarType == CarType.Electric) {
                    car.RequiresProfile = CarProfileRequirement.NoProfile;
                }

                dbHouses.Save(car);
                totalStatisticalNumberOfCars--;
            }

            Info("Remaining cars to allocate: " + totalStatisticalNumberOfCars);
            var householdsByHouseGuid = new Dictionary<string, List<Household>>();
            foreach (var h in houses) {
                var hhs = households.Where(x => x.HouseGuid == h.Guid).ToList();
                householdsByHouseGuid.Add(h.Guid, hhs);
            }


            double adjustmentFactor = 1;
            double summedNumberOfCars = 0;
            var overOrUnder = Direction.Unknown;
            var adjustment = 0.1;
            //approximation to get the right number of cars distributed
            while (Math.Abs(summedNumberOfCars - totalStatisticalNumberOfCars) > 0.1 && adjustment > 0.00001) {
                summedNumberOfCars = 0;
                foreach (var house in houses) {
                    var carProbability = CalcCarProbability(house, adjustmentFactor, householdsByHouseGuid);

                    summedNumberOfCars += carProbability;
                }

                var prevOverOrUnder = overOrUnder;
                if (summedNumberOfCars < totalStatisticalNumberOfCars) {
                    overOrUnder = Direction.Under;
                }
                else if (summedNumberOfCars > totalStatisticalNumberOfCars) {
                    overOrUnder = Direction.Over;
                }

                if (prevOverOrUnder != overOrUnder) {
                    adjustment /= 10;
                }

                if (overOrUnder == Direction.Under) {
                    adjustmentFactor += adjustment;
                }
                else {
                    adjustmentFactor -= adjustment;
                }
            }

            //Info( "Summed:" + summedNumberOfCars + " target: " + totalStatisticalNumberOfCars + " adjustment: " + adjustmentFactor);
            //actually assign the cars
            var actualCarCount = 0;
            var numberOfElectricCars = 10;
            var precreatedCar = new List<Car>();
            for (var i = 0; i < totalStatisticalNumberOfCars; i++) {
                var c = new Car(Guid.NewGuid().ToString(), Services.Rnd.Next(6)) {
                    CarType = CarType.Gasoline
                };
                if (numberOfElectricCars > 0) {
                    c.CarType = CarType.Electric;
                    numberOfElectricCars--;
                }

                precreatedCar.Add(c);
            }

            if (precreatedCar.Count == 0 && totalStatisticalNumberOfCars > 0) {
                throw new Exception("No cars");
            }

            var iterator = 0;
            while (precreatedCar.Count > 0 && iterator < 100) {
                foreach (var house in houses) {
                    var householdsForHouse = householdsByHouseGuid[house.Guid];
                    var carProbability = CalcCarProbability(house, adjustmentFactor, householdsByHouseGuid) / householdsForHouse.Count;
                    for (var i = 0; i < householdsForHouse.Count && precreatedCar.Count > 0; i++) {
                        var d = Services.Rnd.NextDouble();
                        if (d < carProbability) {
                            var household = householdsForHouse[i];
                            var car = precreatedCar[0];
                            car.HouseGuid = house.Guid;
                            precreatedCar.RemoveAt(0);
                            car.HouseholdGuid = household.Guid;
                            if (car.CarType == CarType.Electric) {
                                car.RequiresProfile = CarProfileRequirement.NoProfile;
                            }

                            dbHouses.Save(car);
                            var persistentCar = new PersistentCar(car.CarType, household.HouseholdKey, house.ComplexName, car.Age);
                            dbHousesPersistence.Save(persistentCar);
                            actualCarCount++;
                        }
                    }
                }

                iterator++;
            }

            if (precreatedCar.Count > 0) {
                throw new Exception("there are cars left over. probably a bug? total: " + totalStatisticalNumberOfCars + " left over: " +
                                    precreatedCar.Count);
            }

            dbHousesPersistence.CompleteTransaction();
            dbHouses.CompleteTransaction();
            Info("Number of targeted cars: " + totalStatisticalNumberOfCars + " adjustment factor: " + adjustmentFactor + " actual cars: " +
                 actualCarCount);
        }

        private static double CalcCarProbability([NotNull] House house,
                                                 double adjustmentFactor,
                                                 [NotNull] Dictionary<string, List<Household>> householdsByHouseGuid)
        {
            var households = householdsByHouseGuid[house.Guid];
            double carProbability = 0;
            if (households.Count == 1) {
                carProbability = 0.7 * adjustmentFactor * households.Count;
                if (carProbability > 2) {
                    carProbability = 2;
                }
            }

            if (households.Count == 2) {
                carProbability = 0.7 * adjustmentFactor * households.Count;
                if (carProbability > 3) {
                    carProbability = 3;
                }
            }

            if (households.Count > 2) {
                carProbability = 0.4 * adjustmentFactor * households.Count;
                if (carProbability > households.Count) {
                    carProbability = households.Count + 1;
                }
            }

            return carProbability;
        }
    }
}