using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class D_AssignCars : RunableWithBenchmark {
        public D_AssignCars([NotNull] ServiceRepository services)
            : base(nameof(D_AssignCars), Stage.Houses, 400, services, true, new CarCharts())
        {
        }

        public enum Direction {
            Under,
            Unknown,
            Over
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<Car>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var households = dbHouses.Fetch<Household>();
            var houses = dbHouses.Fetch<House>();
            var occupants = dbHouses.Fetch<Occupant>();
            var householdsByHouseGuid = new Dictionary<string, List<Household>>();
            foreach (var h in houses) {
                var hhs = households.Where(x => x.HouseGuid == h.HouseGuid).ToList();
                householdsByHouseGuid.Add(h.HouseGuid, hhs);
            }

            var totalPeople = occupants.Count;
            if (occupants.Count < 16000) {
                throw new Exception("Too few people found!");
            }

            var totalStatisticalNumberOfCars = (int)(totalPeople * 0.44);

            double adjustmentFactor = 1;
            double summedNumberOfCars = 0;
            var overOrUnder = Direction.Unknown;
            Direction prevOverOrUnder;
            var adjustment = 0.1;
            //approximation to get the right number of cars distributed
            while (Math.Abs(summedNumberOfCars - totalStatisticalNumberOfCars) > 0.1 && adjustment > 0.00001) {
                summedNumberOfCars = 0;
                foreach (var house in houses) {
                    var carProbability = CalcCarProbability(house, adjustmentFactor, householdsByHouseGuid);

                    summedNumberOfCars += carProbability;
                }

                prevOverOrUnder = overOrUnder;
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

            //Log(MessageType.Info, "Summed:" + summedNumberOfCars + " target: " + totalStatisticalNumberOfCars + " adjustment: " + adjustmentFactor);
            //actually assign the cars
            dbHouses.BeginTransaction();
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

            if (precreatedCar.Count == 0) {
                throw new Exception("No cars");
            }

            var iterator = 0;
            while (precreatedCar.Count > 0 && iterator < 100) {
                foreach (var house in houses) {
                    var householdsForHouse = householdsByHouseGuid[house.HouseGuid];
                    var carProbability = CalcCarProbability(house, adjustmentFactor, householdsByHouseGuid) / householdsForHouse.Count;
                    for (var i = 0; i < householdsForHouse.Count && precreatedCar.Count > 0; i++) {
                        var d = Services.Rnd.NextDouble();
                        if (d < carProbability) {
                            var household = householdsForHouse[i];
                            var c = precreatedCar[0];
                            c.HouseGuid = house.HouseGuid;
                            precreatedCar.RemoveAt(0);
                            c.HouseholdGuid = household.HouseholdGuid;
                            dbHouses.Save(c);
                            actualCarCount++;
                        }
                    }
                }

                iterator++;
            }

            if (precreatedCar.Count > 0) {
                throw new Exception("there are cars left over. probably a bug? total: " + totalStatisticalNumberOfCars + " left over: " + precreatedCar.Count);
            }

            dbHouses.CompleteTransaction();
            Log(MessageType.Info, "Number of targeted cars: " + totalStatisticalNumberOfCars + " adjustment factor: " + adjustmentFactor + " actual cars: " + actualCarCount);
        }

        private static double CalcCarProbability([NotNull] House house, double adjustmentFactor, [NotNull] Dictionary<string, List<Household>> householdsByHouseGuid)
        {
            var households = householdsByHouseGuid[house.HouseGuid];
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