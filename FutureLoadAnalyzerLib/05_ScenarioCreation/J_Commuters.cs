using System;
using System.Collections.Generic;
using System.Linq;
using Common.Steps;
using Data.DataModel.Creation;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.Visualisation.SingleSlice;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class J_Commuters : RunableForSingleSliceWithBenchmark {
        public J_Commuters([NotNull] ServiceRepository services) : base(nameof(J_Commuters),
            Stage.ScenarioCreation,
            1000,
            services,
            false,
            new OutgoingCommuterCharts(services, Stage.ScenarioCreation))
        {
            DevelopmentStatus.Add("Not implemented");
            DevelopmentStatus.Add("//todo: properly assign new commuters to the new households");
            DevelopmentStatus.Add("//todo: properly assign new cdes to the new households");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var households = dbDstHouses.FetchAsRepo<Household>();
            dbDstHouses.RecreateTable<OutgoingCommuterEntry>();
            dbDstHouses.RecreateTable<CarDistanceEntry>();
            var srcCarDistanceEntries = dbSrcHouses.FetchAsRepo<CarDistanceEntry>();
            var cars = dbDstHouses.FetchAsRepo<Car>();
            var srcCars = dbSrcHouses.FetchAsRepo<Car>();
            srcCarDistanceEntries.Count.Should().Be(srcCars.Count);
            if (srcCarDistanceEntries.Count == 0) {
                throw new Exception("No car distances were found");
            }

            var carGuids = cars.ToGuidHashset();
            //remove old car distance entries
            List<CarDistanceEntry> toDelete = new List<CarDistanceEntry>();
            foreach (var entry in srcCarDistanceEntries) {
                if (!carGuids.Contains(entry.CarGuid)) {
                    toDelete.Add(entry);
                }
            }

            foreach (var cde in toDelete) {
                srcCarDistanceEntries.Remove(cde);
            }

            //add entries for new cars
            var usedCarGuids = srcCarDistanceEntries.ToReferenceGuidHashset(x => x.CarGuid);
            var carsWithoutCde = new List<Car>();
            foreach (var car in cars) {
                if (!usedCarGuids.Contains(car.Guid)) {
                    carsWithoutCde.Add(car);
                }
            }

            double avgCommuningDistance = srcCarDistanceEntries.Average(x => x.CommutingDistance);
            double avgFreizeitDistance = srcCarDistanceEntries.Average(x => x.FreizeitDistance);
            int newCarcount = 1;
            foreach (var car in carsWithoutCde) {
                var household = households.GetByGuid(car.HouseholdGuid);
                string name = "Car " + newCarcount + " " + slice;
                CarDistanceEntry cde = new CarDistanceEntry(car.HouseGuid,
                    car.HouseholdGuid,
                    car.Guid,
                    avgCommuningDistance,
                    avgFreizeitDistance,
                    new List<int>(),
                    household.FinalIsn,
                    household.HausAnschlussGuid,
                    Guid.NewGuid().ToString(),
                    name,
                    car.CarType);
                newCarcount++;
                srcCarDistanceEntries.Add(cde);
            }

            //change car types
            foreach (var carDistance in srcCarDistanceEntries) {
                var car = cars.GetByGuid(carDistance.CarGuid);
                carDistance.HouseholdGuid = car.HouseholdGuid;
                carDistance.CarType = car.CarType;
            }

            srcCarDistanceEntries.Count.Should().Be(cars.Count);
            srcCarDistanceEntries.SaveAll(dbDstHouses, true, true);
        }
    }
}