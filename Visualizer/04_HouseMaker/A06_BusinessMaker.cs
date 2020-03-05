using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A06_BusinessMaker : RunableWithBenchmark {
        //var fuzzyCalculator = new HouseMemberFuzzyCalc();
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public A06_BusinessMaker([NotNull] ServiceRepository services)
            : base(nameof(A06_BusinessMaker), Stage.Houses, 6, services, false)
        {
            DevelopmentStatus.Add("Make yearly gas use entries properly");
            DevelopmentStatus.Add("Make yearly fernwärme entries properly");
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<BusinessEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;

            //load data
            var potentialBusinesses = dbHouse.Fetch<PotentialBusinessEntry>();
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();
            var houses = dbHouse.Fetch<House>();
            var validIsns = houses.SelectMany(x => x.Hausanschluss.Select(y => y.Isn)).ToHashSet();
            List<int> invalidIsns = new List<int>();
            int randomlyChosenHa = 0;
            int reassignedHAs = 0;
            dbHouse.BeginTransaction();
            foreach (var pb in potentialBusinesses) {
                if (pb.MyCategory == "Immobilien" || pb.MyCategory == "WEG") {
                    if(pb.HighVoltageYearlyElectricityUse > 0) { throw new FlaException("Building infrastructure with MS?");}
                    var pbi = new PotentialBuildingInfrastructure {
                        HouseGuid = pb.HouseGuid,
                        Geschäftspartner = pb.BusinessName,
                        LowVoltageTotalElectricityDemand = pb.LowVoltageYearlyElectricityUse,
                        HighVoltageTotalElectricityDemand = pb.HighVoltageYearlyElectricityUse
                    };
                    dbHouse.Save(pbi);
                }
                else {
                    BusinessType bt = GetTypeFromDescription(pb.MyCategory);
                    var be = new BusinessEntry(pb,bt);
                    var hasIndustry = false;
                    if (pb.LowVoltageLocalnetEntries.Any(x => x.Rechnungsart == "Industrie")|| pb.HighVoltageLocalnetEntries.Any(x => x.Rechnungsart == "Industrie")) {
                        hasIndustry = true;
                    }

                    if (hasIndustry) {
                        be.BusinessType = BusinessType.Industrie;
                    }
                    else {
                        be.BusinessType = GetTypeFromDescription(pb.MyCategory);
                    }
                    //isn kontrolle
                    int validisn = 0;
                    foreach (int hhIsn in be.OriginalISNs)
                    {
                        if (!validIsns.Contains(hhIsn))
                        {
                            invalidIsns.Add(hhIsn);
                        }
                        else
                        {
                            validisn = hhIsn;
                        }
                    }

                    var house = houses.Single(x => x.HouseGuid == be.HouseGuid);
                    if (validisn == 0)
                    {
                        be.FinalIsn = house.Hausanschluss[0].Isn;
                    }
                    else
                    {
                        be.FinalIsn = validisn;
                    }

                    var ha = hausanschlusses.Where(x => x.HouseGuid == be.HouseGuid && x.Isn == be.FinalIsn).ToList();
                    if (ha.Count == 0)
                    {
                        //throw new FlaException("Kein Hausanschluss gefunden.");
                        reassignedHAs++;
                        be.HausAnschlussGuid = house.Hausanschluss[0].HausanschlussGuid;
                    }

                    if (ha.Count == 1)
                    {
                        be.HausAnschlussGuid = ha[0].HausanschlussGuid;
                    }

                    if (ha.Count > 1)
                    {
                        randomlyChosenHa++;
                        be.HausAnschlussGuid = ha[Services.Rnd.Next(ha.Count)].HausanschlussGuid;
                        //throw new FlaException("zu viele Hausanschlüsse gefunden.: " + ha.Count);
                    }
                    dbHouse.Save(be);
                }
            }
            Info("Invalid Isns: " + invalidIsns.Distinct().Count());
            Info("Zufällig ausgewählte Hausanschlüsse bei Häusern mit mehr als einem HA: " + randomlyChosenHa);
            Info("Wohnungen mit neuem Hausanschluss wegen nicht gefundener ISN: " + reassignedHAs);
            dbHouse.CompleteTransaction();
        }

        private BusinessType GetTypeFromDescription([CanBeNull] string category)
        {
            switch (category) {
                case "Haushalt":
                    return BusinessType.Sonstiges;
                case "Büro":
                    return BusinessType.Büro;
                case "Praxis":
                    return BusinessType.Praxis;
                case "Restaurant":
                    return BusinessType.Restaurant;
                case "Werkstatt":
                    return BusinessType.Werkstatt;
                case "Bäcker":
                    return BusinessType.Bäckerei;
                case "Laden":
                    return BusinessType.Shop;
                case "Kirche":
                    return BusinessType.Kirche;
                case "Krankenhaus":
                    return BusinessType.Praxis;
                case "Schule":
                    return BusinessType.Schule;
                case "Senioren":
                    return BusinessType.Seniorenheim;
                case "Fabrik":
                    return BusinessType.Industrie;
                case "Tankstelle":
                    return BusinessType.Tankstelle;
                case "Wasserversorgung":
                    return BusinessType.Wasserversorgung;
                case "Brauerei":
                    return BusinessType.Brauerei;
                case "":
                    return BusinessType.Sonstiges;
                case "Unknown":
                    return BusinessType.Sonstiges;
                case null:
                    return BusinessType.Sonstiges;
                default:
                    Log(MessageType.Error, "unknown category:" + category);
                    throw new Exception("unknown category:[" + category + "]");
            }
        }
    }
}