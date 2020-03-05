using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A06_BusinessMaker : RunableWithBenchmark {
        //var fuzzyCalculator = new HouseMemberFuzzyCalc();
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public A06_BusinessMaker([NotNull] ServiceRepository services) : base(nameof(A06_BusinessMaker), Stage.Houses, 6, services, false)
        {
            DevelopmentStatus.Add("Make yearly gas use entries properly");
            DevelopmentStatus.Add("Make yearly fernwärme entries properly");
        }

        protected override void RunActualProcess()
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouse.RecreateTable<BusinessEntry>();
            //load data
            var potentialBusinesses = dbHouse.Fetch<PotentialBusinessEntry>();
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();
            var houses = dbHouse.Fetch<House>();
            var validIsns = houses.SelectMany(x => x.Hausanschluss.Select(y => y.Isn)).ToHashSet();
            List<int> invalidIsns = new List<int>();
            const int randomlyChosenHa = 0;
            const int reassignedHAs = 0;
            List<BusinessEntry> businesses = new List<BusinessEntry>();
            dbHouse.BeginTransaction();
            foreach (var pb in potentialBusinesses) {
                if ((pb.MyCategory == "Immobilien" || pb.MyCategory == "WEG") &&
                    pb.Standort != "Einschlagweg 59, Geschoss unbekannt, 3400 Burgdorf" &&
                    pb.Standort != "Fabrikweg 6, Geschoss unbekannt, 3400 Burgdorf") {
                    if (pb.HighVoltageYearlyElectricityUse > 0) {
                        throw new FlaException("Building infrastructure with MS?");
                    }

                    var pbi = new PotentialBuildingInfrastructure(pb.HouseGuid,
                        pb.BusinessName,
                        pb.LowVoltageYearlyElectricityUse,
                        pb.HighVoltageYearlyElectricityUse,
                        pb.LowVoltageLocalnetEntries,
                        pb.HighVoltageLocalnetEntries,
                        pb.Standort, Guid.NewGuid().ToString());
                    dbHouse.Save(pbi);
                }
                else {
                    BusinessType bt = GetTypeFromDescription(pb.MyCategory);
                    var be = new BusinessEntry(pb, bt);
                    be.HouseComponentType = HouseComponentType.BusinessNoLastgangLowVoltage;
                    //var hasIndustry = false;
                    /*if (pb.LowVoltageLocalnetEntries.Any(x => x.Rechnungsart == "Industrie") ||
                        pb.HighVoltageLocalnetEntries.Any(x => x.Rechnungsart == "Industrie")) {
                        hasIndustry = true;
                    }*/

/*                    if (hasIndustry) {
                        be.BusinessType = BusinessType.Industrie;
                    }
                    else {
                        ;
                    }*/

                    //isn kontrolle
                    int validisn = 0;
                    foreach (int hhIsn in be.OriginalISNs) {
                        if (!validIsns.Contains(hhIsn)) {
                            invalidIsns.Add(hhIsn);
                        }
                        else {
                            validisn = hhIsn;
                        }
                    }

                    var house = houses.Single(x => x.Guid == be.HouseGuid);
                    if (validisn == 0) {
                        be.FinalIsn = house.Hausanschluss[0].Isn;
                    }
                    else {
                        be.FinalIsn = validisn;
                    }


                    var ha = hausanschlusses.Single(x => x.Guid == pb.HausAnschlussGuid);
                    if (ha == null) {
                        throw new FlaException("ha was null");
                    }

                    /*     hausanschlusses.Where(x => x.HouseGuid == be.HouseGuid && x.Isn == be.FinalIsn).ToList();
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
                       }*/
                    be.HausAnschlussGuid = pb.HausAnschlussGuid;
                    businesses.Add(be);
                }
            }
            dbHouse.CompleteTransaction();
            AssignRlmProfiles(businesses, houses);
            foreach (var businessEntry in businesses) {
                if (businessEntry.LocalnetHighVoltageYearlyTotalElectricityUse > 0) {
                    if (businessEntry.HouseComponentType == HouseComponentType.BusinessNoLastgangLowVoltage) {
                        businessEntry.HouseComponentType = HouseComponentType.BusinessNoLastgangHighVoltage;
                    }
                    else if (businessEntry.HouseComponentType == HouseComponentType.BusinessWithLastgangLowVoltage) {
                        businessEntry.HouseComponentType = HouseComponentType.BusinessWithLastgangHighVoltage;
                    }
                }
            }

            Debug("Invalid Isns: " + invalidIsns.Distinct().Count());
            Debug("Zufällig ausgewählte Hausanschlüsse bei Häusern mit mehr als einem HA: " + randomlyChosenHa);
            Debug("Wohnungen mit neuem Hausanschluss wegen nicht gefundener ISN: " + reassignedHAs);
            int normalBusinesses = businesses.Count(x => x.HouseComponentType == HouseComponentType.BusinessNoLastgangLowVoltage);
            int rlmBusinesses = businesses.Count(x => x.HouseComponentType == HouseComponentType.BusinessWithLastgangLowVoltage);
            Debug("Businesses without rlm:" + normalBusinesses + " with: " + rlmBusinesses);
            dbHouse.BeginTransaction();
            foreach (BusinessEntry entry in businesses) {
                dbHouse.Save(entry);
            }

            dbHouse.CompleteTransaction();
            RowCollection rc = new RowCollection("Businesses", "");
            foreach (var entry in businesses) {
                RowBuilder rb = RowBuilder.Start("Name",entry.BusinessName);
                rb.Add("Verbrauchstyp", entry.EnergyType);
                rb.Add("Verbrauch", entry.EffectiveEnergyDemand);
                rb.Add("Businesstype", entry.BusinessType.ToString());
                rc.Add(rb);
            }

            var fn = MakeAndRegisterFullFilename("Businesses.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn,rc);
        }

        private void AssignRlmProfiles([NotNull] [ItemNotNull] List<BusinessEntry> businesses, [NotNull] [ItemNotNull] List<House> houses)
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var assignments = dbRaw.Fetch<LastgangBusinessAssignment>();
            foreach (var assignment in assignments) {
                if (assignment.BusinessName == "none") {
                    continue;
                }

                //pv anlagen
                if (!string.IsNullOrWhiteSpace(assignment.ErzeugerID)) {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(assignment.ComplexName)
                    && !string.IsNullOrWhiteSpace(assignment.BusinessName)) {
                    var selectedBusinesses = businesses.Where(x => x.BusinessName == assignment.BusinessName).ToList();
                    if (selectedBusinesses.Count == 0) {
                        throw new Exception("Not a single business was found: " + assignment.BusinessName);
                    }
                    var rightbusiness = selectedBusinesses[0];
                    if (assignment.Standort != null && selectedBusinesses.Count > 1) {
                        rightbusiness = selectedBusinesses.Single(x => x.Standort == assignment.Standort);
                        if (rightbusiness == null) {
                            throw new FlaException("No business found with standort: " + assignment.Standort + " And complex " + assignment.ComplexName);
                        }
                    }else

                    if (selectedBusinesses.Count > 1) {
                        var rightHouse = houses.Single(x => x.ComplexName == assignment.ComplexName);
                        selectedBusinesses = selectedBusinesses.Where(x => x.HouseGuid == rightHouse.Guid).ToList();
                        rightbusiness = selectedBusinesses[0];
                    }

                    if (!string.IsNullOrWhiteSpace(assignment.Standort)) {
                        if (rightbusiness.Standort != assignment.Standort) {
                            throw new FlaException("wrong assignement: Standort incorrect");
                        }
                    }
                    rightbusiness.HouseComponentType = HouseComponentType.BusinessWithLastgangLowVoltage;
                    if (!string.IsNullOrWhiteSpace( rightbusiness.RlmProfileName)) {
                        throw new FlaException("profile was already assigned: "+ rightbusiness.RlmProfileName);
                    }
                    rightbusiness.RlmProfileName = assignment.RlmFilename ?? throw new InvalidOperationException();
                    /*// ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    // ReSharper disable once HeuristicUnreachableCode
                    if (rightbusiness.HausAnschlussGuid == null) {
                        throw new FlaException("No hausanschluss");
                    }*/
                }
                else {
                    throw new FlaException("Found a rlm profile without any identifying info: " + assignment.RlmFilename);
                }
            }
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
                case "Hotel":
                    return BusinessType.Hotel;
                case "Hallenbad":
                    return BusinessType.Hallenbad;
                case "Eissport":
                    return BusinessType.Eissport;
                case "Museum":
                    return BusinessType.Museum;
                case "Mobilfunk":
                    return BusinessType.Mobilfunk;
                case "":
                    return BusinessType.Sonstiges;
                case "Unknown":
                    return BusinessType.Sonstiges;
                case "Immobilien":
                    return BusinessType.Sonstiges;
                case null:
                    return BusinessType.Sonstiges;
                default:
                    Error("unknown category:" + category);
                    throw new Exception("unknown category:[" + category + "]");
            }

        }
    }
}