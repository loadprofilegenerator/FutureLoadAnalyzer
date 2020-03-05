using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class C_OccupantCalculation : RunableWithBenchmark {
        //place people
        public C_OccupantCalculation([NotNull] ServiceRepository services)
            : base(nameof(C_OccupantCalculation), Stage.Houses, 300, services, true,
                new OccupantCharts())
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<Occupant>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var households = dbHouses.Fetch<Household>();
            if (households.Count == 0) {
                throw new Exception("no households founds");
            }

            var hmfc = new HouseMemberFuzzyCalc(Services.MyLogger);
            var potentialPersons = new List<PotentialPerson>();
            var jahrgänge = dbRaw.Fetch<Jahrgang>();
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
            dbHouses.BeginTransaction();
            foreach (var household in households) {
                household.HeuristicFamiliySize = hmfc.GetPeopleCountForEnergy(household.LowVoltageYearlyTotalElectricityUse);
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
                dbHouses.Save(occ2);
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
                    dbHouses.Save(occ3);
                    allocatedCount++;
                }

                if (allocatedCount == 0 && potentialPersons.Count > 0) {
                    var hhs = households.Where(x => x.HeuristicFamiliySize > 2).ToList();
                    var idx = Services.Rnd.Next(hhs.Count);
                    hhs[idx].HeuristicFamiliySize++;
                }
            }

            foreach (var hh in households) {
                dbHouses.Save(hh);
            }

            dbHouses.CompleteTransaction();
        }

        [NotNull]
        private static Occupant MakeOccupant([ItemNotNull] [NotNull] List<PotentialPerson> eligiablePersons, [NotNull] Random r, [ItemNotNull] [NotNull] List<PotentialPerson> potentialPersons,
                                             [NotNull] Household potentialHousehold)
        {
            var pp = eligiablePersons[r.Next(eligiablePersons.Count)];
            potentialPersons.Remove(pp);
            var occ = new Occupant {
                Age = pp.Age,
                Gender = pp.Gender,
                HouseholdGuid = potentialHousehold.HouseholdGuid,
                OccupantGuid = Guid.NewGuid().ToString(),
                HouseGuid = potentialHousehold.HouseGuid
            };
            return occ;
        }

        public class PotentialPerson {
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
