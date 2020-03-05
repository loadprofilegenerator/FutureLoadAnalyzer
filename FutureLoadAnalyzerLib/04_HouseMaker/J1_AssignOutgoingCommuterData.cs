using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using Visualizer.Visualisation.SingleSlice;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class J1_AssignOutgoingCommuterData : RunableWithBenchmark {
        public J1_AssignOutgoingCommuterData([JetBrains.Annotations.NotNull] ServiceRepository services) : base(nameof(J1_AssignOutgoingCommuterData),
            Stage.Houses,
            1000,
            services,
            false,
            new OutgoingCommuterCharts(services, Stage.Houses))
        {
            DevelopmentStatus.Add("//TODO: make sure to only assign commutingMethod Car to people with cars!!!)");
            DevelopmentStatus.Add("//https://www.bfs.admin.ch/bfs/de/home/statistiken/mobilitaet-verkehr/personenverkehr/pendlermobilitaet.html");
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var dbHousesPersistence =
                Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice, DatabaseCode.Persistence);
            dbHousesPersistence.CreateTableIfNotExists<PersistentOutgoingCommuterEntry>();
            dbHouses.RecreateTable<OutgoingCommuterEntry>();
            dbHouses.RecreateTable<CarDistanceEntry>();
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var outGoings = dbRaw.Fetch<OutgoingCommuterSummary>();
            var households = dbHouses.Fetch<Household>();
            var residents = households.SelectMany(x => x.Occupants).ToList().AsReadOnly();
            if (residents.Count == 0) {
                throw new Exception("No occupants found");
            }

            var persistentOutgoingCommuters = dbHousesPersistence.Fetch<PersistentOutgoingCommuterEntry>();
            var cars = dbHouses.Fetch<Car>();
            var householdGuids = households.Select(x => x.Guid).ToList();
            foreach (var car in cars) {
                if (!householdGuids.Contains(car.HouseholdGuid)) {
                    throw new FlaException("car with invalid household guid");
                }
            }

            if (cars.Count < 1000) {
                throw new FlaException("Not a single car was loaded");
            }

            Debug("total planned commuters before persistence: " + outGoings.Sum(x => x.Erwerbstätige));
            int idx = 0;
            int deletecount = 0;
            dbHouses.BeginTransaction();
            dbHousesPersistence.BeginTransaction();
            foreach (var persistentOutgoingCommuterEntry in persistentOutgoingCommuters) {
                var household = households.FirstOrDefault(x => x.HouseholdKey == persistentOutgoingCommuterEntry.HouseholdKey);
                if (household == null) {
                    dbHousesPersistence.Delete(persistentOutgoingCommuterEntry);
                    continue;
                }

                Car car = cars.FirstOrDefault(x => x.HouseholdGuid == household.Guid);
                if (car == null && persistentOutgoingCommuterEntry.CommuntingMethod == CommuntingMethod.Car) {
                    Debug("deleted inconsistent commuter " + deletecount++);
                    dbHousesPersistence.Delete(persistentOutgoingCommuterEntry);
                    continue;
                }

                OutgoingCommuterEntry ogce = new OutgoingCommuterEntry(Guid.NewGuid().ToString(),
                    household.Guid,
                    persistentOutgoingCommuterEntry.DistanceInKm,
                    persistentOutgoingCommuterEntry.CommuntingMethod,
                    persistentOutgoingCommuterEntry.WorkCity,
                    persistentOutgoingCommuterEntry.WorkKanton,
                    household.HouseGuid);

                dbHouses.Save(ogce);
                var outgoing = outGoings.Single(x =>
                    x.Arbeitsgemeinde == persistentOutgoingCommuterEntry.WorkCity && x.Arbeitskanton == persistentOutgoingCommuterEntry.WorkKanton);
                outgoing.Erwerbstätige--;
                if (outgoing.Erwerbstätige < 0) {
                    throw new FlaException("Negative workers");
                }

                if (car != null) {
                    //bus & bahn
                    cars.Remove(car);
                    CarDistanceEntry cde = new CarDistanceEntry(household.HouseGuid,
                        household.Guid,
                        car.Guid,
                        persistentOutgoingCommuterEntry.DistanceInKm,
                        27.4,
                        household.OriginalISNs,
                        household.FinalIsn,
                        household.HausAnschlussGuid,
                        Guid.NewGuid().ToString(),
                        "Car " + (idx++),
                        car.CarType);
                    dbHouses.Save(cde);
                }
            }

            dbHouses.CompleteTransaction();
            dbHousesPersistence.CompleteTransaction();
            Debug("total planned commuters after persistence: " + outGoings.Sum(x => x.Erwerbstätige));
            var outgoingCommuters = new List<OutgoingCommuterEntry>();

            foreach (var commuter in outGoings) {
                for (var i = 0; i < commuter.Erwerbstätige; i++) {
                    var oce = new OutgoingCommuterEntry {
                        CommuterGuid = Guid.NewGuid().ToString()
                    };
                    var method = Services.Rnd.NextDouble();

                    if (method < 0.52) {
                        oce.CommuntingMethod = CommuntingMethod.Car;
                    }
                    else {
                        oce.CommuntingMethod = CommuntingMethod.PublicTransport;
                    }

                    oce.DistanceInKm = commuter.Entfernung;
                    oce.WorkCity = commuter.Arbeitsgemeinde;
                    oce.WorkKanton = commuter.Arbeitskanton;
                    outgoingCommuters.Add(oce);
                }
            }

            //freizeit km nach mikromobilitätszensus, 12.05 auto-km/tag/person, 0.44 autos pro person = 27.39 km/tag
            var potentialResidents = residents.Where(x => x.Age > 18 && x.Age < 65).ToList();
            var householdGuidsWithCar = cars.Select(x => x.HouseholdGuid).ToList();
            var potentialResidentsWithCar = potentialResidents.Where(x => householdGuidsWithCar.Contains(x.HouseholdGuid)).ToList();
            var potentialResidentsWithoutCar = potentialResidents.Where(x => !potentialResidentsWithCar.Contains(x)).ToList();
            List<Car> availableCars = cars.ToList();
            //assign the commuters
            List<OutgoingCommuterEntry> processedOutgoingCommuters = new List<OutgoingCommuterEntry>();
            dbHouses.BeginTransaction();
            while (outgoingCommuters.Count > 0) {
                OutgoingCommuterEntry ogce = outgoingCommuters[0];
                processedOutgoingCommuters.Add(ogce);
                outgoingCommuters.RemoveAt(0);
                Occupant occupant = null;

                if (ogce.CommuntingMethod == CommuntingMethod.Car) {
                    Car myCar = null;

                    while (myCar == null) {
                        occupant = potentialResidentsWithCar[Services.Rnd.Next(potentialResidentsWithCar.Count)];
                        potentialResidentsWithCar.Remove(occupant);
                        myCar = availableCars.FirstOrDefault(x => x.HouseholdGuid == occupant.HouseholdGuid);
                    }

                    availableCars.Remove(myCar);

                    Household hh = households.FirstOrDefault(x => x.Guid == occupant.HouseholdGuid);
                    if (hh == null) {
                        throw new FlaException("No household for " + ogce.HouseholdGuid);
                    }

                    CarDistanceEntry cd = new CarDistanceEntry(occupant.HouseGuid,
                        occupant.HouseholdGuid,
                        myCar.Guid,
                        ogce.DistanceInKm,
                        27.4,
                        hh.OriginalISNs,
                        hh.FinalIsn,
                        hh.HausAnschlussGuid,
                        Guid.NewGuid().ToString(),
                        (idx++).ToString(),
                        myCar.CarType);
                    dbHouses.Save(cd);
                }
                else {
                    if (potentialResidentsWithoutCar.Count > 0) {
                        occupant = potentialResidentsWithoutCar[Services.Rnd.Next(potentialResidentsWithoutCar.Count)];
                        potentialResidentsWithoutCar.Remove(occupant);
                    }
                    else {
                        occupant = potentialResidentsWithCar[Services.Rnd.Next(potentialResidentsWithCar.Count)];
                        potentialResidentsWithCar.Remove(occupant);
                    }
                }

                ogce.HouseholdGuid = occupant.HouseholdGuid;
                ogce.HouseGuid = occupant.HouseGuid;
                ogce.CommuterGuid = Guid.NewGuid().ToString();
            }

            //the other cars are only used for freizeit / shopping / etc
            foreach (Car car in availableCars) {
                Household hh = households.Single(x => x.Guid == car.HouseholdGuid);
                CarDistanceEntry cd = new CarDistanceEntry(car.HouseGuid,
                    car.HouseholdGuid,
                    car.Guid,
                    0,
                    27.4,
                    hh.OriginalISNs,
                    hh.FinalIsn,
                    hh.HausAnschlussGuid,
                    Guid.NewGuid().ToString(),
                    "Car " + (idx++),
                    car.CarType);
                dbHouses.Save(cd);
            }

            foreach (var entry in processedOutgoingCommuters) {
                if (string.IsNullOrWhiteSpace(entry.HouseholdGuid)) {
                    throw new FlaException("Household guid was empty");
                }

                var household = households.Single(x => x.Guid == entry.HouseholdGuid);
                PersistentOutgoingCommuterEntry poce = new PersistentOutgoingCommuterEntry(household.HouseholdKey,
                    entry.DistanceInKm,
                    entry.CommuntingMethod,
                    entry.WorkCity,
                    entry.WorkKanton);
                dbHousesPersistence.Save(poce);
                dbHouses.Save(entry);
            }

            dbHousesPersistence.CompleteTransaction();
            dbHouses.CompleteTransaction();
        }
    }
}