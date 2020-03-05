using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A05_HouseholdMaker : RunableWithBenchmark {
        //var fuzzyCalculator = new HouseMemberFuzzyCalc();
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public A05_HouseholdMaker([NotNull] ServiceRepository services)
            : base(nameof(A05_HouseholdMaker), Stage.Houses, 5, services, true, new HouseholdCharts())
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<Household>(Stage.Houses, Constants.PresentSlice);
            const int householdAccordingToStadtverwaltung = 8081;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //load data
            var potentialHouseholds = dbHouse.Fetch<PotentialHousehold>();
            var houses = dbHouse.Fetch<House>();
            var hausanschlusses = dbHouse.Fetch < Hausanschluss>();
            var validIsns = houses.SelectMany(x => x.Hausanschluss.Select(y => y.Isn)).ToHashSet();
            Info("Total valid isns: " + validIsns.Count);
            potentialHouseholds.Sort((x, y) => y.YearlyElectricityUse.CompareTo(x.YearlyElectricityUse));
            var count = 0;
            if (potentialHouseholds.Count < householdAccordingToStadtverwaltung) {
                throw new Exception("Not enough potential households found: potential:" + potentialHouseholds.Count + " needed minimum: " + householdAccordingToStadtverwaltung);
            }
            List<int> invalidIsns = new List<int>();
            dbHouse.BeginTransaction();
            int randomlyChosenHa = 0;
            int reassignedHAs = 0;
            foreach (var potentialHousehold in potentialHouseholds) {
                if (count < householdAccordingToStadtverwaltung) {
                    //make household
                    var hh = new Household(potentialHousehold);
                    int validisn = 0;
                    foreach (int hhIsn in hh.OriginalISNs) {
                        if (!validIsns.Contains(hhIsn)) {
                            invalidIsns.Add(hhIsn);
                        }
                        else {
                            validisn = hhIsn;
                        }
                    }

                    var house = houses.Single(x => x.HouseGuid == hh.HouseGuid);
                    if (validisn == 0) {
                        hh.FinalIsn = house.Hausanschluss[0].Isn;
                    }
                    else {
                        hh.FinalIsn = validisn;
                    }

                    var ha = hausanschlusses.Where(x => x.HouseGuid == hh.HouseGuid && x.Isn == hh.FinalIsn).ToList();
                    if (ha.Count == 0) {
                        //throw new FlaException("Kein Hausanschluss gefunden.");
                        reassignedHAs++;
                        hh.HausAnschlussGuid = house.Hausanschluss[0].HausanschlussGuid;
                    }

                    if (ha.Count == 1) {
                        hh.HausAnschlussGuid = ha[0].HausanschlussGuid;
                    }

                    if (ha.Count > 1) {
                        randomlyChosenHa++;
                        hh.HausAnschlussGuid = ha[Services.Rnd.Next(ha.Count)].HausanschlussGuid;
                        //throw new FlaException("zu viele Hausanschlüsse gefunden.: " + ha.Count);
                    }
                    dbHouse.Save(hh);
                }
                else {
                    var pbi = new PotentialBuildingInfrastructure {
                        HouseGuid = potentialHousehold.HouseGuid,
                        Geschäftspartner = potentialHousehold.BusinessPartnerName,
                        LowVoltageTotalElectricityDemand = potentialHousehold.YearlyElectricityUse
                    };
                    dbHouse.Save(pbi);
                }

                count++;
            }
            Info("Invalid Isns: " + invalidIsns.Distinct().Count());
            Info("Zufällig ausgewählte Hausanschlüsse bei Häusern mit mehr als einem HA: " + randomlyChosenHa);
            Info("Wohnungen mit neuem Hausanschluss wegen nicht gefundener ISN: " +reassignedHAs);
            dbHouse.CompleteTransaction();
        }

    }
}