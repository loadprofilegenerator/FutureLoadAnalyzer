using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class C_OccupantCalculation : RunableWithBenchmark {
        //place people
        public C_OccupantCalculation([NotNull] ServiceRepository services) : base(nameof(C_OccupantCalculation),
            Stage.Houses,
            300,
            services,
            true,
            new OccupantCharts(services, Stage.Houses))
        {
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var dbHousesPersistence =
                Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice, DatabaseCode.Persistence);
            dbHousesPersistence.CreateTableIfNotExists<PersistentHouseholdResidents>();
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var households = dbHouses.Fetch<Household>();
            var jahrgänge = dbRaw.Fetch<Jahrgang>();
            if (households.Count == 0) {
                throw new Exception("no households founds");
            }

            if (households.Count != A05_HouseholdMaker.HouseholdAccordingToStadtverwaltung) {
                throw new FlaException("Missing households!?");
            }

            foreach (var household in households) {
                household.Occupants.Clear();
            }
            var persistentResidents = dbHousesPersistence.Fetch<PersistentHouseholdResidents>();
            int householdCountBefore = households.Count;
            dbHouses.BeginTransaction();
            Info("found " + persistentResidents.Count + " persistent residents");
            foreach (var persistentHouseholdResidentse in persistentResidents) {
                var household = households.FirstOrDefault(x => x.HouseholdKey == persistentHouseholdResidentse.HouseholdKey);
                if (household == null) {
                    Info("Invalid persistence for " + persistentHouseholdResidentse.HouseholdKey);
                    dbHousesPersistence.Delete(persistentHouseholdResidentse);
                    continue;
                }

                foreach (var persistentOccupant in persistentHouseholdResidentse.Occupants) {
                    var occupant = new Occupant(household.Guid,
                        Guid.NewGuid().ToString(),
                        persistentOccupant.Age,
                        persistentOccupant.Gender,
                        household.HouseGuid,
                        household.HouseholdKey);
                    household.Occupants.Add(occupant);
                    var jahrgang = 2018 - persistentOccupant.Age;
                    var einträge = jahrgänge.Single(x => x.Jahr == jahrgang);
                    einträge.Count--;
                    if (einträge.Count < 0) {
                        throw new FlaException("Negative population");
                    }
                }

                dbHouses.Save(household);
                households.Remove(household);
            }

            Info("Covered " + (householdCountBefore - households.Count) + " households from persistence, households left: " + households.Count);

            var hmfc = new HouseMemberFuzzyCalc(Services.MyLogger, MyStage);
            var potentialPersons = new List<PotentialPerson>();
            foreach (var jahrgang in jahrgänge) {
                var age = 2018 - jahrgang.Jahr;
                var g = Gender.Male;
                for (var i = 0; i < jahrgang.Count; i++) {
                    var pp = new PotentialPerson(g, age);
                    potentialPersons.Add(pp);
                    g = g == Gender.Male ? Gender.Female : Gender.Male;
                }
            }

            var r = new Random(1);
            foreach (var household in households) {
                household.HeuristicFamiliySize = hmfc.GetPeopleCountForEnergy(household.EffectiveEnergyDemand);
                household.Occupants.Clear();
            }

            //put at least one person into every household
            foreach (var household in households) {
                var eligiablePersons = potentialPersons.Where(x => x.Age >= 18).ToList();
                var occ = MakeOccupant(eligiablePersons, r, potentialPersons, household);
                household.Occupants.Add(occ);
                dbHouses.Save(occ);
            }

            //put a second person into the households that might have a second one
            foreach (var household in households) {
                if (household.HeuristicFamiliySize < 2) {
                    continue;
                }

                var g = household.Occupants[0].Gender;
                var otherGender = g == Gender.Male ? Gender.Female : Gender.Male;
                var eligiablePersons = potentialPersons.Where(x => x.Age >= 18 && x.Gender == otherGender).ToList();
                if (eligiablePersons.Count == 0) {
                    eligiablePersons = potentialPersons;
                }

                var occ2 = MakeOccupant(eligiablePersons, r, potentialPersons, household);
                household.Occupants.Add(occ2);
            }

            var count = 0;
            while (potentialPersons.Count > 0) {
                count++;
                if (count > 100000) {
                    throw new Exception("Couldnt allocate everything after " + count + " iterations," + potentialPersons.Count + " left.");
                }

                var allocatedCount = 0;
                foreach (var household in households) {
                    if (household.Occupants.Count >= household.HeuristicFamiliySize) {
                        continue;
                    }

                    if (potentialPersons.Count == 0) {
                        break;
                    }

                    var eligiablePersonsKids = potentialPersons.Where(x => x.Age < 18).ToList();
                    if (eligiablePersonsKids.Count == 0) {
                        eligiablePersonsKids = potentialPersons;
                    }

                    var occ3 = MakeOccupant(eligiablePersonsKids, r, potentialPersons, household);
                    household.Occupants.Add(occ3);
                    allocatedCount++;
                }

                if (allocatedCount == 0 && potentialPersons.Count > 0) {
                    var hhs = households.Where(x => x.HeuristicFamiliySize > 2).ToList();
                    if (hhs.Count == 0) {
                        hhs = households;
                    }

                    var idx = Services.Rnd.Next(hhs.Count);
                    hhs[idx].HeuristicFamiliySize++;
                }
            }

            List<PersistentHouseholdResidents> newPersistentResidents = new List<PersistentHouseholdResidents>();
            int peopleCount = 0;
            foreach (var hh in households) {
                dbHouses.Save(hh);
                PersistentHouseholdResidents phhr = new PersistentHouseholdResidents(hh.HouseholdKey);

                foreach (var occupant in hh.Occupants) {
                    phhr.Occupants.Add(new PersistentOccupant(occupant.Age, occupant.Gender));
                    peopleCount++;
                }

                newPersistentResidents.Add(phhr);
            }

            dbHouses.CompleteTransaction();
            dbHousesPersistence.BeginTransaction();
            foreach (var phhr in newPersistentResidents) {
                dbHousesPersistence.Save(phhr);
            }

            Info("Saved " + newPersistentResidents.Count + " persistence records with a total of " + peopleCount + " people");
            dbHousesPersistence.CompleteTransaction();
            var allhouseholds = dbHouses.FetchAsRepo<Household>();
            var rc = new RowCollection("occupants","occupants");
            foreach (var hh in allhouseholds) {
                foreach (var occupant in hh.Occupants) {
                    var rb = RowBuilder.Start("age", occupant.Age).Add("Gender", occupant.Gender);
                    rc.Add(rb);
                }
            }

            var fn = MakeAndRegisterFullFilename("OccupantList.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn,rc);
        }

        [NotNull]
        private static Occupant MakeOccupant([ItemNotNull] [NotNull] List<PotentialPerson> eligiablePersons,
                                             [NotNull] Random r,
                                             [ItemNotNull] [NotNull] List<PotentialPerson> potentialPersons,
                                             [NotNull] Household potentialHousehold)
        {
            var pp = eligiablePersons[r.Next(eligiablePersons.Count)];
            potentialPersons.Remove(pp);
            var occ = new Occupant(potentialHousehold.Guid,
                Guid.NewGuid().ToString(),
                pp.Age,
                pp.Gender,
                potentialHousehold.HouseGuid,
                potentialHousehold.HouseholdKey);
            return occ;
        }

        private class PotentialPerson {
            public PotentialPerson(Gender gender, int age)
            {
                Gender = gender;
                Age = age;
            }

            public int Age { get; set; }

            public Gender Gender { get; set; }
        }
    }
}