using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A05_HouseholdMaker : RunableWithBenchmark {
        public const int HouseholdAccordingToStadtverwaltung = 8081;

        //var fuzzyCalculator = new HouseMemberFuzzyCalc();
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public A05_HouseholdMaker([NotNull] ServiceRepository services) : base(nameof(A05_HouseholdMaker),
            Stage.Houses,
            5,
            services,
            true,
            new HouseholdCharts(services, Stage.Houses))
        {
        }

        protected override void RunActualProcess()
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouse.RecreateTable<Household>();
            //load data
            var potentialHouseholds = dbHouse.Fetch<PotentialHousehold>();
            var houses = dbHouse.Fetch<House>();
            var validIsns = houses.SelectMany(x => x.Hausanschluss.Select(y => y.Isn)).ToHashSet();
            Debug("Total valid isns: " + validIsns.Count);
            potentialHouseholds.Sort((x, y) => y.YearlyElectricityUse.CompareTo(x.YearlyElectricityUse));
            var count = 0;
            if (potentialHouseholds.Count < HouseholdAccordingToStadtverwaltung) {
                throw new Exception("Not enough potential households found: potential:" + potentialHouseholds.Count + " needed minimum: " +
                                    HouseholdAccordingToStadtverwaltung);
            }

            dbHouse.BeginTransaction();
            const int randomlyChosenHa = 0;
            const int reassignedHAs = 0;
            int chosenHouseholds = 0;
            foreach (var potentialHousehold in potentialHouseholds) {
                if (count < HouseholdAccordingToStadtverwaltung) {
                    //make household
                    var hh = new Household(potentialHousehold);
                    chosenHouseholds++;
                    dbHouse.Save(hh);
                }
                else {
                    var pbi = new PotentialBuildingInfrastructure(potentialHousehold.HouseGuid,
                        potentialHousehold.BusinessPartnerName,
                        potentialHousehold.YearlyElectricityUse,
                        0,
                        potentialHousehold.LocalnetEntries,
                        new List<Localnet>(),
                        potentialHousehold.Standort, Guid.NewGuid().ToString());
                    dbHouse.Save(pbi);
                }

                count++;
            }

            if (chosenHouseholds != HouseholdAccordingToStadtverwaltung) {
                throw new FlaException("Wrong number of households");
            }

            Debug("Zufällig ausgewählte Hausanschlüsse bei Häusern mit mehr als einem HA: " + randomlyChosenHa);
            Debug("Wohnungen mit neuem Hausanschluss wegen nicht gefundener ISN: " + reassignedHAs);
            dbHouse.CompleteTransaction();
        }
    }
}