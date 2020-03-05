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

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class A02_ScnHouseholds : RunableForSingleSliceWithBenchmark {
        public A02_ScnHouseholds([NotNull] ServiceRepository services) : base(nameof(A02_ScnHouseholds),
            Stage.ScenarioCreation,
            101,
            services,
            false,
            new HouseholdCharts(services, Stage.ScenarioCreation))
        {
            DevelopmentStatus.Add("add additional households to the existing houses");
            DevelopmentStatus.Add("Remove emigrating households");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            Info("running house copying");
            //kill off old people
            //save killed old people
            //delete households and remember now free households
            //put new people into households

            Info("Aging occupants");
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var srcHouseholds = dbSrcHouses.Fetch<Household>();
            var allOccupants = srcHouseholds.SelectMany(x => x.Occupants).ToList().AsReadOnly();
            foreach (var srcOccupant in allOccupants) {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // ReSharper disable once HeuristicUnreachableCode
                if (srcOccupant.HouseholdKey == null) {
                    // ReSharper disable once HeuristicUnreachableCode
                    throw new FlaException("Householdkey was null");
                }
            }

            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var dstHouses = dbDstHouses.Fetch<House>();
            var hausanschlusses = dbDstHouses.Fetch<Hausanschluss>();
            double energyReductionFactor = slice.EnergyReductionFactorHouseholds;
            if (Math.Abs(energyReductionFactor) < 0.000001) {
                throw new FlaException("Factor 0 for household energy reduction in slice " + slice);
            }

            foreach (var srcHousehold in srcHouseholds) {
                srcHousehold.LocalnetEntries.Clear();
                srcHousehold.SetEnergyReduction(slice.DstYear.ToString(), srcHousehold.EffectiveEnergyDemand * (1 - energyReductionFactor));
            }

            var yearsToAge = slice.DstYear - slice.PreviousSlice?.DstYear ?? throw new FlaException("No Previous Scenario Set");
            foreach (var occ in allOccupants) {
                occ.Age += yearsToAge;
            }

            ApplyDeathsIncludingPersistence(slice, srcHouseholds, allOccupants);

            srcHouseholds = PutPeopleIntoTheHouseholds(allOccupants, srcHouseholds);

            //create new people
            BirthNewChildren(slice, srcHouseholds);

            //allocate additional households
            foreach (var srcHousehold in srcHouseholds) {
                if (srcHousehold.Occupants.Count == 0) {
                    throw new FlaException("household with no occupants found ");
                }
            }

            var householdsByHouseGuid = new Dictionary<string, List<Household>>();
            foreach (var household in srcHouseholds) {
                if (!householdsByHouseGuid.ContainsKey(household.HouseGuid)) {
                    householdsByHouseGuid.Add(household.HouseGuid, new List<Household>());
                }

                householdsByHouseGuid[household.HouseGuid].Add(household);
            }

            List<HouseWithHouseholds> allhwhs = new List<HouseWithHouseholds>();
            foreach (var house in dstHouses) {
                HouseWithHouseholds hwh;
                if (householdsByHouseGuid.ContainsKey(house.Guid)) {
                    hwh = new HouseWithHouseholds(house, householdsByHouseGuid[house.Guid]);
                }
                else {
                    hwh = new HouseWithHouseholds(house, new List<Household>());
                }

                allhwhs.Add(hwh);
            }

            List<Household> newHouseholds = new List<Household>();

            var persistenceDb = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice, DatabaseCode.Persistence);
            persistenceDb.CreateTableIfNotExists<PersistenceMigrationHouseholdEntry>();
            persistenceDb.CreateTableIfNotExists<PersistenceMigratedOccupant>();
            var persistencenewHouseholdEntries = persistenceDb.Fetch<PersistenceMigrationHouseholdEntry>();

            var migrants = persistenceDb.Fetch<PersistenceMigratedOccupant>();
            int targetPopulation = (int)slice.TargetPopulationAfterMigration;
            int exisitingPopulation = srcHouseholds.Sum(x => x.Occupants.Count);
            int numberOfPeopleMovingIn = targetPopulation - exisitingPopulation;
            if (numberOfPeopleMovingIn < 0) {
                throw new FlaException("Targetpopulation below natural population count. Add more deaths. Target was " + targetPopulation +
                                       " natural population was : " + exisitingPopulation + " in the slice " + slice + " diff: " +
                                       numberOfPeopleMovingIn);
            }

            if (numberOfPeopleMovingIn < migrants.Count) {
                migrants = migrants.Take(numberOfPeopleMovingIn).ToList();
            }

            var persistanceMigrantsHouseKeys = migrants.Select(x => x.HouseholdKey).Distinct().ToList();
            persistenceDb.BeginTransaction();
            var householdsWithNewlyAddedPeople = new List<Household>();
            foreach (var nhhe in persistencenewHouseholdEntries) {
                var hwh = allhwhs.FirstOrDefault(x => x.House.ComplexName == nhhe.HouseName);
                if (hwh == null) {
                    persistenceDb.Delete(nhhe);
                    continue;
                }

                if (hwh.RemainingOpenHouseholds == 0) {
                    double avgEbf = hwh.House.Appartments.Average(x => x.EnergieBezugsFläche);
                    hwh.House.Appartments.Add(new AppartmentEntry(Guid.NewGuid().ToString(), avgEbf, true, slice.DstYear));
                }

                var hausanschluss = hwh.House.GetHausanschlussByIsn(new List<int>(), null, hausanschlusses, MyLogger) ??
                                    throw new FlaException("no hausanschluss");
                var hh = new Household(nhhe.Name,
                    hwh.House.Guid,
                    hausanschluss.Guid,
                    hwh.House.ComplexName,
                    hausanschluss.Standort,
                    Guid.NewGuid().ToString());
                if (!persistanceMigrantsHouseKeys.Contains(hh.HouseholdKey)) {
                    Info("Deleted persistance household because no persistent occupants could be found");
                    persistenceDb.Delete(nhhe);
                    continue;
                }

                if (newHouseholds.Any(x => x.HouseholdKey == hh.HouseholdKey)) {
                    Info("Deleted persistance household because duplicated key");
                    persistenceDb.Delete(nhhe);
                    continue;
                }

                hwh.Households.Add(hh);

                newHouseholds.Add(hh);
                householdsWithNewlyAddedPeople.Add(hh);
            }

            //haushalte anlegen

            int migratedPeople = 0;

            var emptyHouseholds = newHouseholds.ToList();
            var newHouseholdKeys = newHouseholds.Select(x => x.HouseholdKey).Distinct().ToHashSet();
            var migrantsToDelete = migrants.Where(x => !newHouseholdKeys.Contains(x.HouseholdKey)).ToList();
            foreach (var persistenceMigratedOccupant in migrantsToDelete) {
                persistenceDb.Delete(persistenceMigratedOccupant);
                Info("Deleted persistence migrant due to missing house");
                migrants.Remove(persistenceMigratedOccupant);
            }

            //person migration
            Info("Number of new migratants before persistence: " + numberOfPeopleMovingIn);
            foreach (var migrant in migrants) {
                var newEmptyHousehold = emptyHouseholds.First(x => x.HouseholdKey == migrant.HouseholdKey);
                Occupant occ = new Occupant(newEmptyHousehold.Guid,
                    Guid.NewGuid().ToString(),
                    migrant.Age,
                    migrant.Gender,
                    newEmptyHousehold.HouseGuid,
                    newEmptyHousehold.HouseholdKey);
                migratedPeople++;
                newEmptyHousehold.Occupants.Add(occ);
                householdsWithNewlyAddedPeople.Add(newEmptyHousehold);
                numberOfPeopleMovingIn--;
            }

            foreach (var household in newHouseholds) {
                if (household.Occupants.Count == 0) {
                    throw new FlaException("empty household found");
                }
            }

            if (numberOfPeopleMovingIn < 0) {
                throw new FlaException("negative people moving in");
            }

            Info("Number of new migratants after persistence: " + numberOfPeopleMovingIn);
            Info("Number of empty households after persistence: " + emptyHouseholds.Count);

            for (int i = 0; i < numberOfPeopleMovingIn; i++) {
                //find a household for the persons
                Household pickedHousehold;
                if (emptyHouseholds.Count > 0) {
                    pickedHousehold = emptyHouseholds[0];
                    emptyHouseholds.RemoveAt(0);
                }
                else {
                    pickedHousehold = MakeSingleNewHousehold(slice, hausanschlusses, i, persistenceDb, allhwhs, newHouseholds);
                    newHouseholds.Add(pickedHousehold);
                }

                householdsWithNewlyAddedPeople.Add(pickedHousehold);
                Occupant occ = new Occupant(pickedHousehold.Guid,
                    Guid.NewGuid().ToString(),
                    Services.Rnd.Next(60),
                    (Gender)Services.Rnd.Next(2),
                    pickedHousehold.HouseGuid,
                    pickedHousehold.HouseholdKey);
                migratedPeople++;
                PersistenceMigratedOccupant moc = new PersistenceMigratedOccupant(occ.HouseholdKey, occ.Age, occ.Gender);
                persistenceDb.Save(moc);
                pickedHousehold.Occupants.Add(occ);
            }


            foreach (var hh in householdsWithNewlyAddedPeople.Distinct()) {
                if (Math.Abs(hh.LocalnetLowVoltageYearlyTotalElectricityUse) > 0.0001) {
                    throw new FlaException("Household with energy consumption already set");
                }

                double energyconsumption = 1000 + hh.Occupants.Count * 500 + Services.Rnd.Next(1000) - 500;
                hh.LocalnetLowVoltageYearlyTotalElectricityUse = energyconsumption;
            }

            Info("Migrators: " + migratedPeople);
            persistenceDb.CompleteTransaction();
            dbDstHouses.RecreateTable<Household>();
            dbDstHouses.BeginTransaction();
            int householdsSaved = 0;
            // save old households
            foreach (var srchh in srcHouseholds) {
                srchh.Occupants.Sort((x, y) => x.Age.CompareTo(y.Age));
                if (srchh.Occupants.Count == 0) {
                    throw new FlaException("No people left in the hh");
                }

                if (Math.Abs(srchh.LocalnetLowVoltageYearlyTotalElectricityUse) < 0.00001) {
                    throw new FlaException("Household without energy consumption found: " + srchh.Name);
                }

                dbDstHouses.Save(srchh);
                householdsSaved++;
            }

            // save new households
            foreach (var newHousehold in newHouseholds) {
                newHousehold.Occupants.Sort((x, y) => x.Age.CompareTo(y.Age));
                if (newHousehold.Occupants.Count == 0) {
                    throw new FlaException("No people in the new hh");
                }

                dbDstHouses.Save(newHousehold);
                householdsSaved++;
            }

            if (householdsSaved == 0) {
                throw new FlaException("Not a single household saved");
            }

            Info("finished writing " + householdsSaved + " households");
            foreach (var house in dstHouses) {
                dbDstHouses.Save(house);
            }

            dbDstHouses.CompleteTransaction();
            Info("finished house copying");
        }

        private void ApplyDeathsIncludingPersistence([NotNull] ScenarioSliceParameters slice,
                                                     [NotNull] [ItemNotNull]
                                                     List<Household> srcHouseholds,
                                                     [NotNull] [ItemNotNull]
                                                     IReadOnlyCollection<Occupant> allOccupants)
        {
            WeightedRandomAllocator<Occupant> wra = new WeightedRandomAllocator<Occupant>(Services.Rnd, Services.Logger);
            var dbDstHousePersistence = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice, DatabaseCode.Persistence);
            dbDstHousePersistence.CreateTableIfNotExists<PersistenceDeathEntry>();
            int numberOfDeathsLeft = (int)slice.NumberOfDeaths;
            var deathEntries = dbDstHousePersistence.Fetch<PersistenceDeathEntry>();
            var peopleToRemove = new List<Occupant>();
            Info("Number of planned deaths: " + numberOfDeathsLeft);
            dbDstHousePersistence.BeginTransaction();
            foreach (var de in deathEntries) {
                var hh = srcHouseholds.FirstOrDefault(x => x.HouseholdKey == de.HouseholdKey);
                if (hh == null) {
                    dbDstHousePersistence.Delete(de);
                    continue;
                }

                var srcocc = allOccupants.FirstOrDefault(x => x.HouseholdGuid == hh.Guid && x.Age == de.Age && x.Gender == de.Gender);
                if (srcocc == null) {
                    dbDstHousePersistence.Delete(de);
                    continue;
                }

                numberOfDeathsLeft--;
                peopleToRemove.Add(srcocc);
                if (numberOfDeathsLeft == 0) {
                    break;
                }
            }

            Info("Deaths after applying persistent deaths from db: " + numberOfDeathsLeft);
            List<Occupant> randomRemovedPeople = new List<Occupant>();
            if (numberOfDeathsLeft > 0) {
                randomRemovedPeople = wra.PickNumberOfObjects(allOccupants, (x => x.Age * x.Age), numberOfDeathsLeft, true);
                Info("Randomly picked additional people: " + randomRemovedPeople.Count);
            }

            peopleToRemove.AddRange(randomRemovedPeople);
            Info("People dying: " + peopleToRemove.Count);
            foreach (var deadocc in peopleToRemove) {
                Household hh = srcHouseholds.First(x => x.Guid == deadocc.HouseholdGuid);
                PersistenceDeathEntry de = new PersistenceDeathEntry(hh.HouseholdKey, deadocc.Age, deadocc.Gender);
                dbDstHousePersistence.Save(de);
                hh.Occupants.Remove(deadocc);
            }

            dbDstHousePersistence.CompleteTransaction();
        }

        private void BirthNewChildren([NotNull] ScenarioSliceParameters slice,
                                      [NotNull] [ItemNotNull]
                                      List<Household> srcHouseholds)
        {
            //find households to add children to
            var householdsThatCouldHaveChildren = new List<Household>();
            foreach (var household in srcHouseholds) {
                bool hasMom = household.Occupants.Any(x => x.Age > 18 && x.Age < 45 && x.Gender == Gender.Female);
                bool hasDad = household.Occupants.Any(x => x.Age > 18 && x.Age < 60 && x.Gender == Gender.Male);
                if (hasDad && hasMom) {
                    householdsThatCouldHaveChildren.Add(household);
                }
            }

            int numberOfChildren = (int)slice.NumberOfChildren;
            Info("Number of children before persistence: " + numberOfChildren);
            int peopleBorn = 0;
            var persistenceDb = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice, DatabaseCode.Persistence);
            persistenceDb.CreateTableIfNotExists<PersistenceBirthEntry>();
            var birthEntries = persistenceDb.Fetch<PersistenceBirthEntry>();
            foreach (var birthEntry in birthEntries) {
                var householdToAddChildTo = householdsThatCouldHaveChildren.FirstOrDefault(x => x.HouseholdKey == birthEntry.HouseholdKey);
                if (householdToAddChildTo == null) {
                    persistenceDb.Delete(birthEntry);
                    continue;
                }

                Occupant occ = new Occupant(householdToAddChildTo.Guid,
                    Guid.NewGuid().ToString(),
                    1,
                    birthEntry.Gender,
                    householdToAddChildTo.HouseGuid,
                    householdToAddChildTo.HouseholdKey);
                householdToAddChildTo.Occupants.Add(occ);
                peopleBorn++;
                numberOfChildren--;
                if (numberOfChildren < 1) {
                    break;
                }
            }

            Info("Number of children after persistence: " + numberOfChildren);
            if (householdsThatCouldHaveChildren.Count == 0) {
                throw new FlaException("Not a single household available to have children?");
            }

            //give birth to the children
            persistenceDb.BeginTransaction();
            for (int i = 0; i < numberOfChildren; i++) {
                var pickedHousehold = householdsThatCouldHaveChildren[Services.Rnd.Next(householdsThatCouldHaveChildren.Count)];
                var gender = (Gender)Services.Rnd.Next(2);
                Occupant occ = new Occupant(pickedHousehold.Guid,
                    Guid.NewGuid().ToString(),
                    1,
                    gender,
                    pickedHousehold.HouseGuid,
                    pickedHousehold.HouseholdKey);
                pickedHousehold.Occupants.Add(occ);
                PersistenceBirthEntry be = new PersistenceBirthEntry(occ.HouseholdKey, occ.Gender);
                persistenceDb.Save(be);
                peopleBorn++;
            }

            persistenceDb.CompleteTransaction();
            Info("Children: " + peopleBorn);
        }

        [NotNull]
        private Household MakeSingleNewHousehold([NotNull] ScenarioSliceParameters slice,
                                                 [NotNull] [ItemNotNull]
                                                 List<Hausanschluss> hausanschlusses,
                                                 int i,
                                                 [NotNull] MyDb persistenceDb,
                                                 [NotNull] [ItemNotNull]
                                                 List<HouseWithHouseholds> allhwhs,
                                                 [NotNull] [ItemNotNull]
                                                 List<Household> newHouseholds)
        {
            if (allhwhs == null) {
                throw new ArgumentNullException(nameof(allhwhs));
            }

            var filteredhwhs = allhwhs.Where(x => x.RemainingOpenHouseholds > 0).ToList();
            if (filteredhwhs.Count == 0) {
                var potentialHwhForExpansion = allhwhs.Where(x => x.Households.Count > 0 && x.RemainingOpenHouseholds == 0).ToList();
                if (potentialHwhForExpansion.Count == 0) {
                    potentialHwhForExpansion = allhwhs.Where(x => x.Households.Count > 0).ToList();
                    if (potentialHwhForExpansion.Count == 0) {
                        throw new FlaException("Could not find anything for adding hhs");
                    }
                }

                var pickedhwh = potentialHwhForExpansion[Services.Rnd.Next(potentialHwhForExpansion.Count)];
                double avgEbf = pickedhwh.House.Appartments.Average(x => x.EnergieBezugsFläche);
                while (pickedhwh.RemainingOpenHouseholds < 1) {
                    pickedhwh.House.Appartments.Add(new AppartmentEntry(Guid.NewGuid().ToString(), avgEbf, true, slice.DstYear));
                }

                pickedhwh.House.Appartments.Add(new AppartmentEntry(Guid.NewGuid().ToString(), avgEbf, true, slice.DstYear));
                filteredhwhs = allhwhs.Where(x => x.RemainingOpenHouseholds > 0).ToList();
                if (filteredhwhs.Count == 0) {
                    throw new FlaException("?");
                }

            }

            var hwh = filteredhwhs[Services.Rnd.Next(filteredhwhs.Count)];
            Hausanschluss hausanschluss = hwh.House.GetHausanschlussByIsn(new List<int>(), null, hausanschlusses, MyLogger) ??
                                          throw new FlaException("no hausanschluss");
            var hh = new Household("new hh " + i + " from " + slice,
                hwh.House.Guid,
                hausanschluss.Guid,
                hwh.House.ComplexName,
                hausanschluss.Standort,
                Guid.NewGuid().ToString());
            PersistenceMigrationHouseholdEntry nhhe = new PersistenceMigrationHouseholdEntry(hwh.House.ComplexName, hh.Name, hausanschluss.ObjectID);
            persistenceDb.Save(nhhe);
            hwh.Households.Add(hh);

            newHouseholds.Add(hh);
            return hh;
        }

        [NotNull]
        [ItemNotNull]
        private static List<Household> PutPeopleIntoTheHouseholds([NotNull] [ItemNotNull]
                                                                  IReadOnlyCollection<Occupant> allOccupants,
                                                                  [NotNull] [ItemNotNull]
                                                                  List<Household> srcHouseholds)
        {
            var occupantsByhhGuid = new Dictionary<string, List<Occupant>>();
            foreach (Occupant occupant in allOccupants) {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // ReSharper disable once HeuristicUnreachableCode
                if (occupant.HouseholdGuid == null) {
                    // ReSharper disable once HeuristicUnreachableCode
                    throw new FlaException("household guid was null");
                }

                if (!occupantsByhhGuid.ContainsKey(occupant.HouseholdGuid)) {
                    occupantsByhhGuid.Add(occupant.HouseholdGuid, new List<Occupant>());
                }

                occupantsByhhGuid[occupant.HouseholdGuid].Add(occupant);
            }

            foreach (var hh in srcHouseholds) {
                hh.Occupants.Clear(); //clear the occupants to make it easier to update later
                hh.ID = 0;
                if (occupantsByhhGuid.ContainsKey(hh.Guid)) {
                    hh.Occupants = occupantsByhhGuid[hh.Guid];
                }
            }

            //filter out the empty households
            srcHouseholds = srcHouseholds.Where(x => x.Occupants.Count > 0).ToList();
            return srcHouseholds;
        }

        private class HouseWithHouseholds {
            public HouseWithHouseholds([NotNull] House house,
                                       [NotNull] [ItemNotNull]
                                       List<Household> allHouseholds)
            {
                House = house;
                Households = allHouseholds;
            }

            [NotNull]
            public House House { get; }

            [NotNull]
            [ItemNotNull]
            public List<Household> Households { get; }

            public int RemainingOpenHouseholds => House.OfficialNumberOfHouseholds - Households.Count;
        }
    }
}