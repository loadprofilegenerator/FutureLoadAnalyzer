using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using JetBrains.Annotations;
using NPoco;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class A04_LocalnetProcessor : RunableWithBenchmark {
        //[ItemNotNull] [NotNull] private readonly List<string> _alreadyRegisteredPotentialBusinesses = new List<string>();


        public A04_LocalnetProcessor([NotNull] ServiceRepository services)
            : base(nameof(A04_LocalnetProcessor), Stage.Houses, 4, services, false)
        {
            DevelopmentStatus.Add("//todo: implement monthly energy demand for PotentialBuildingInfrastructure");
            DevelopmentStatus.Add("//todo: implement localnet energy entries for PotentialBuildingInfrastructure");
            DevelopmentStatus.Add("//TODO: sankey mit strom für industrie, handel, haushalte");
            DevelopmentStatus.Add("//TODO: sankey mit gas für industrie, handel, haushalte");
            DevelopmentStatus.Add("//TODO: sankey mit wärme für industrie, handel, haushalte");
            DevelopmentStatus.Add("//todo: figure out sumemr base gas use and summer base electricity use");
        }

        protected override void RunActualProcess()
        {
            //tables
            SqlConnection.RecreateTable<PotentialHousehold>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<PotentialBusinessEntry>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<PotentialHeatingSystemEntry>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<SuspiciousBusinessEntry>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<PotentialBuildingInfrastructure>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<StreetLightingEntry>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<HouseSummedLocalnetEnergyUse>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<PotentialCookingSystemEntry>(Stage.Houses, Constants.PresentSlice);
            //databases
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //load data
            var buildingcomplexes = dbComplex.Fetch<BuildingComplex>();
            var businesses = dbRaw.Fetch<BusinessName>();
            var houses = dbHouse.Fetch<House>();
            var localnet = dbRaw.Fetch<Localnet>();
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();

            //initialize dictionaries
            var complexesByID = new Dictionary<string, BuildingComplex>();
            foreach (var complex in buildingcomplexes) {
                complexesByID.Add(complex.ComplexGuid, complex);
            }

            var localnetByStandort = new Dictionary<string, List<Localnet>>();
            foreach (var localnet1 in localnet) {
                if (!localnetByStandort.ContainsKey(localnet1.Objektstandort ?? throw new InvalidOperationException())) {
                    localnetByStandort.Add(localnet1.Objektstandort, new List<Localnet>());
                }

                localnetByStandort[localnet1.Objektstandort].Add(localnet1);
            }

            var alreadyRegisteredPotentialBusinesses = new List<string>();
            dbHouse.BeginTransaction();
            foreach (var h in houses) {
                var complex = complexesByID[h.ComplexGuid];
                //standort analysis für jeden standort initialisieren
                var standorte = complex.ObjektStandorte;
                var standortAnalysises = new List<StandortAnalysis>();
                foreach (var standort in standorte) {
                    var locanetforStandort = localnetByStandort[standort];
                    if (locanetforStandort.Count == 0) {
                        throw new Exception("No entries for standort " + standort);
                    }

                    var geschäftspartnerNames = locanetforStandort.Select(x => x.VertragspartnerAdresse).Distinct().ToList();
                    foreach (var gs in geschäftspartnerNames) {
                        List<Localnet> localnetForGs = locanetforStandort.Where(x => x.VertragspartnerAdresse == gs).ToList();


                        var sa = new StandortAnalysis(localnetForGs, Services.MyLogger,
                            standort, gs, dbHouse, businesses,
                            alreadyRegisteredPotentialBusinesses);
                        standortAnalysises.Add(sa);
                    }
                }

                var hsl = new HouseSummedLocalnetEnergyUse {
                    HouseGuid = h.HouseGuid
                };

                foreach (var sa in standortAnalysises) {
                    //try to figure out for each standort what it is.
                    //find all localnet entries
                    if ((sa.LowVoltageTotalElectricity+sa.HighVoltageTotalElectricity) > 0) {
                        hsl.ElectricityUse += sa.LowVoltageTotalElectricity + sa.HighVoltageTotalElectricity;
                        hsl.ElectricityUseDayHigh += sa.HighVoltageElectricityUseDaytime;
                        hsl.ElectricityUseDayLow += sa.LowVoltageElectricityUseDaytime;
                        hsl.ElectricityUseNightHigh += sa.HighVoltageElectricityUseNighttime;
                        hsl.ElectricityUseNightLow += sa.LowVoltageElectricityUseNighttime;
                        double sum = hsl.ElectricityUseDayHigh + hsl.ElectricityUseDayLow + hsl.ElectricityUseNightHigh + hsl.ElectricityUseNightLow;
                        if(Math.Abs(hsl.ElectricityUse - sum) > 0.00001)
                        {
                            throw new FlaException("invalid addition");
                        }

                        if (sa.Rechnungsart == "Oeffentliche Beleuchtung") {
                            var sle = new StreetLightingEntry {
                                HouseGuid = h.HouseGuid,
                                YearlyElectricityUse = sa.LowVoltageTotalElectricity + sa.HighVoltageTotalElectricity,
                                LightingGuid = Guid.NewGuid().ToString()
                            };
                            dbHouse.Save(sle);
                        }
                        else if (sa.BusinessCategory == "Immobilien") {
                            var bi = MakePotentialBuildingInfrastructureEntry(h, sa);
                            dbHouse.Save(bi);
                        }
                        else if (sa.IsBusiness()) {
                            var be = MakeBusinessEntry(sa, h,hausanschlusses);
                            dbHouse.Save(be);
                        }
                        else if (sa.Rechnungsart == "Haushalt (P 42/45/48/51)" || sa.Rechnungsart == "Haushalt (P 43/46/49/52)" || sa.Rechnungsart == "Haushalt (P 41/44/47/50)") {
                            var hh = MakePotentialHouseholds(h, sa,hausanschlusses);
                            dbHouse.Save(hh);
                        }
                        else {
                            throw new Exception("Unknown rechnungsart: " + sa.Rechnungsart);
                        }
                    }

                    if (sa.GasUse > 0) {
                        if (sa.GasUse > 1000) {
                            var hse = new PotentialHeatingSystemEntry(h.HouseGuid, Guid.NewGuid().ToString(),sa.IsnID) {
                                YearlyGasDemand = sa.GasUse,
                                HeatingSystemType = HeatingSystemType.GasheatingLocalnet
                            };
                            dbHouse.Save(hse);
                        }
                        else {
                            var hse = new PotentialCookingSystemEntry(h.HouseGuid, Guid.NewGuid().ToString(), sa.Standort) {
                                YearlyGasDemand = sa.GasUse,
                                HeatingSystemType = HeatingSystemType.GasheatingLocalnet
                            };
                            dbHouse.Save(hse);
                        }

                        hsl.GasUse += sa.GasUse;
                    }

                    if (sa.FernWärme > 0) {
                        var hse = new PotentialHeatingSystemEntry(h.HouseGuid, Guid.NewGuid().ToString(),sa.IsnID) {
                            YearlyFernwärmeDemand = sa.FernWärme,
                            HeatingSystemType = HeatingSystemType.FernwärmeLocalnet
                        };
                        dbHouse.Save(hse);
                        hsl.WärmeUse += sa.FernWärme;
                    }
                }

                dbHouse.Save(hsl);
            }

            dbHouse.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var potentialHouseholds = dbHouse.Fetch<PotentialHousehold>();
            var potentialBusinesses = dbHouse.Fetch<PotentialBusinessEntry>();
            MakePotentialHouseholdsMap(Constants.PresentSlice);
            MakePotentialBusinessMap();
            EnergySankey(Constants.PresentSlice);
            EnergySankeyTrafokreis(Constants.PresentSlice);

            void MakePotentialHouseholdsMap(ScenarioSliceParameters slice)
            {
                var ssa = new SingleSankeyArrow("Potential Households", 1000,
                    MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Potential Households total", potentialHouseholds.Count, 5000, Orientation.Straight));
                var potentialHouseholdsEnergySmaller1000 = potentialHouseholds.Count(x => x.YearlyElectricityUse < 1000);
                var potentialHouseholdsEnergySmaller2000 = potentialHouseholds.Count(x => x.YearlyElectricityUse < 2000 && x.YearlyElectricityUse >= 1000);
                var potentialHouseholdsEnergySmaller5000 = potentialHouseholds.Count(x => x.YearlyElectricityUse < 5000 && x.YearlyElectricityUse >= 2000);
                var potentialHouseholdsEnergyGreater5000 = potentialHouseholds.Count(x => x.YearlyElectricityUse > 5000);
                ssa.AddEntry(new SankeyEntry("Haushalte mit Energie < 1000", potentialHouseholdsEnergySmaller1000 * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Haushalte mit Energie < 2000", potentialHouseholdsEnergySmaller2000 * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Haushalte mit Energie < 5000", potentialHouseholdsEnergySmaller5000 * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Haushalte mit Energie > 5000", potentialHouseholdsEnergyGreater5000 * -1, 5000, Orientation.Down));


                Services.PlotMaker.MakeSankeyChart(ssa);

                RGB GetColor(House h)
                {
                    var s = potentialHouseholds.Where(x => x.HouseGuid == h.HouseGuid).ToString();
                    if (s.Length == 0) {
                        return new RGB(255, 0, 0);
                    }

                    if (h.GebäudeObjectIDs.Count == 1) {
                        return new RGB(0, 0, 255);
                    }

                    return new RGB(0, 255, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHouseholdCountsPerHouse.svg", Name, "", Constants.PresentSlice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("No Household", 255, 0, 0),
                    new MapLegendEntry("Genau 1 Haushalt", 0, 0, 255),
                    new MapLegendEntry("Viele Haushalte", 0, 255, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }


            void MakePotentialBusinessMap()
            {
                RGB GetColor(House h)
                {
                    var s = potentialBusinesses.Where(x => x.HouseGuid == h.HouseGuid).ToList();
                    if (s.Count == 0) {
                        return new RGB(255, 0, 0);
                    }

                    if (s.Count == 1) {
                        return new RGB(0, 0, 255);
                    }

                    return new RGB(0, 255, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapBusinessCountsPerHouse.svg", Name, "",Constants.PresentSlice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("No business", 255, 0, 0),
                    new MapLegendEntry("Genau 1 business", 0, 0, 255),
                    new MapLegendEntry("Viele geschäfte", 0, 255, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }

            void EnergySankey(ScenarioSliceParameters slice)
            {
                var hhs = dbHouse.Fetch<PotentialHousehold>();
                var business = dbHouse.Fetch<PotentialBusinessEntry>();
                var buildingInfrastructures = dbHouse.Fetch<PotentialBuildingInfrastructure>();
                var streetlights = dbHouse.Fetch<StreetLightingEntry>();
                const double fac = 1_000_000;
                var hhsum = hhs.Sum(x => x.YearlyElectricityUse) / fac;
                var businessum = business.Sum(x => x.LowVoltageYearlyElectricityUse + x.HighVoltageYearlyElectricityUse) / fac;
                var infrasum = buildingInfrastructures.Sum(x => x.LowVoltageTotalElectricityDemand + x.HighVoltageTotalElectricityDemand) / fac;
                var streetsum = streetlights.Sum(x => x.YearlyElectricityUse) / fac;
                var sum = hhsum + businessum + infrasum + streetsum;
                var ssa = new SingleSankeyArrow("AufgeteilterStromverbrauch", 1000,
                    MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Gesamt", sum, 500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Haushalte", hhsum * -1, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Business", businessum * -1, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Buildings", infrasum * -1, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Beleuchtung", streetsum * -1, 500, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void EnergySankeyTrafokreis(ScenarioSliceParameters slice)
            {
                var hhs = dbHouse.Fetch<PotentialHousehold>();
                var business = dbHouse.Fetch<PotentialBusinessEntry>();
                var buildingInfrastructures = dbHouse.Fetch<PotentialBuildingInfrastructure>();
                var streetlights = dbHouse.Fetch<StreetLightingEntry>();
                const double fac = 1_000_000;
                var houseguids = houses.Where(x => !string.IsNullOrWhiteSpace(x.TrafoKreis)).Select(x => x.HouseGuid).ToList();
                var hhsum = hhs.Where(x => houseguids.Contains(x.HouseGuid)).Sum(x => x.YearlyElectricityUse) / fac;
                var businessum = business.Where(x => houseguids.Contains(x.HouseGuid)).Sum(x => x.LowVoltageYearlyElectricityUse + x.HighVoltageYearlyElectricityUse) / fac;
                var infrasum = buildingInfrastructures.Where(x => houseguids.Contains(x.HouseGuid)).Sum(x => x.LowVoltageTotalElectricityDemand + x.HighVoltageTotalElectricityDemand) / fac;
                var streetsum = streetlights.Where(x => houseguids.Contains(x.HouseGuid)).Sum(x => x.YearlyElectricityUse) / fac;
                var sum = hhsum + businessum + infrasum + streetsum;
                var ssa = new SingleSankeyArrow("AufgeteilterStromverbrauchMitTrafokreis", 1000, MyStage,
                    SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Gesamt", sum, 500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Haushalte", hhsum * -1, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Business", businessum * -1, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Buildings", infrasum * -1, 500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Beleuchtung", streetsum * -1, 500, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }
        }

        [CanBeNull]
        private static string GetGeschäftspartnerCategory([ItemNotNull] [NotNull] List<BusinessName> businesses, [NotNull] string gsOrig, [NotNull] Database dbHouse,
                                                          [ItemNotNull] [NotNull] List<string> alreadyRegisteredPotentialBusinesses, double electricityuse, double gasUse, double wärmeUse)
        {
            var gs = gsOrig.Trim().ToLowerInvariant();
            // ReSharper disable once PossibleNullReferenceException
            var businessName = businesses.Where(x => x.Name.Trim().ToLowerInvariant() == gs).ToList();
            if (businessName.Count > 1) {
                throw new Exception("Duplicate gs entry: " + gs);
            }

            if (businessName.Count == 1) {
                return businessName[0].Category;
            }

            var suspiciousStrings = new List<string> {
                "gmbh",
                " ag,",
                "laden",
                "restaurant",
                "baudirektion",
                "gemeinde",
                "zivilschutz",
                "febacom",
                "architekten",
                "verband",
                "& co. ag",
                "beckerstube",
                "eisenhandlung",
                "stockwerkeigentum",
                "berner kmu",
                "stiftung",
                "zentrum",
                "coop",
                "migros",
                "pensionskasse",
                "stweg",
                "finanzdirektion",
                "erbengemeinschaft",
                "contact",
                "gesellschaft",
                "stowe",
                "niederlassung",
                "versicherung",
                "vorsorge",
                "genossenschaft",
                "pro burgdorf",
                "immobilier",
                "sarl",
                "sàrl",
                "salt mobile",
                "abteilung",
                "department",
                "STWE",
                "styleo klg",
                "hairfasion",
                "immobilien",
                "bank",
                "praxis",
                "pro senectute",
                "pro infirmis",
                "physiotherapie",
                "coiffeur",
                "holding",
                "bekb",
                "notar",
                "miteigentümer",
                "productions",
                "neumarkt",
                "meg bahnhofstrasse",
                "pflegeheim",
                "lebensart",
                "le bars",
                "bäckerei",
                "administration",
                "erben",
                "kindergarten",
                "treuhand",
                "heizung",
                "reparatur",
                "kanton",
                "unternehm",
                "handelsmühle",
                "a.g.",
                "heilsarmee",
                "flüchtling",
                "habidom",
                "gymnasium",
                "brasserie",
                "fahrschule",
                "fahrlehrer",
                "geschäft",
                "pizza",
                "evang",
                "therapie",
                "trinkwasser",
                "eigenverbrauchsgemeinschaft",
                "eigentümergemeinschaft",
                "gemeinschaft",
                "e-gestion sa",
                " sa,",
                "design",
                "boutique",
                "confiserie",
                "coifför",
                "mode",
                "blumen ",
                "blumenhandwerk",
                "arche ",
                "alterssiedlung",
                "agentur",
                "region",
                "büro",
                "garage",
                "schweizerische post"
            };
            var lowerName = gs.ToLowerInvariant().Trim();
            if (!alreadyRegisteredPotentialBusinesses.Contains(gsOrig) && suspiciousStrings.Any(x => lowerName.Contains(x))) {
                var pbe = new SuspiciousBusinessEntry {
                    Name = gsOrig,
                    Electricity = electricityuse,
                    GasUse = gasUse,
                    WärmeUse = wärmeUse
                };
                alreadyRegisteredPotentialBusinesses.Add(gsOrig);
                dbHouse.Save(pbe);
            }

            return null;
        }

        [NotNull]
        private static PotentialBusinessEntry MakeBusinessEntry([NotNull] StandortAnalysis sa, [NotNull] House h, [NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses)
        {
            var houseGuid = h.HouseGuid;
            var businessGuid = Guid.NewGuid().ToString();
            var businessName = sa.BusinessPartnerName;
            var complexName = h.ComplexName;
            var lowVoltageYearlyElectricityUse = sa.LowVoltageTotalElectricity;
            var highVoltageYearlyElectricityUse = sa.HighVoltageTotalElectricity;
            var lowVoltageYearlyElectricityUseDaytime = sa.LowVoltageElectricityUseDaytime;
            var lowVoltageYearlyElectricityUseNighttime = sa.LowVoltageElectricityUseNighttime;

            string hausanschlussGuid = hausanschlusses.Single(x => x.Isn == sa.IsnID).HausanschlussGuid;
            var be = new PotentialBusinessEntry(houseGuid, businessGuid, businessName, complexName, lowVoltageYearlyElectricityUse, highVoltageYearlyElectricityUse,
                lowVoltageYearlyElectricityUseDaytime, lowVoltageYearlyElectricityUseNighttime, sa.GasUse, sa.FernWärme,
                sa.SummerLowVoltageBaseElectricity,sa.SummerBaseGasUse, false,
                -1,sa.LocalNetEntries.Count,sa.ElectricityTarif,sa.BusinessCategory, hausanschlussGuid);
            be.Standorte.Add(sa.Standort);
            be.LowVoltageLocalnetEntries = sa.LowVoltageElectricityEntries;
            be.NumberOfLocalnetEntries = sa.LowVoltageElectricityEntries.Count + sa.HighVoltageElectricityEntries.Count;
            be.Tarif = sa.ElectricityTarif;
            return be;
        }

        [NotNull]
        private static PotentialBuildingInfrastructure MakePotentialBuildingInfrastructureEntry([NotNull] House h, [NotNull] StandortAnalysis sa)
        {
            var bi = new PotentialBuildingInfrastructure {
                HouseGuid = h.HouseGuid,
                Geschäftspartner = sa.BusinessPartnerName,
                LowVoltageTotalElectricityDemand = sa.LowVoltageTotalElectricity,
                HighVoltageTotalElectricityDemand = sa.HighVoltageTotalElectricity
            };
            return bi;
        }

        [NotNull]
        private static PotentialHousehold MakePotentialHouseholds([NotNull] House h, [NotNull] StandortAnalysis sa, [NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses)
        {
            var houseGuid = h.HouseGuid;
            var householdGuid = Guid.NewGuid().ToString();
            double yearlyElectricityUse = sa.LowVoltageTotalElectricity;
            var tarif = sa.ElectricityTarif;
            var numberOfLocalnetEntries = sa.LowVoltageElectricityEntries.Count;
            var businessPartnerName = sa.BusinessPartnerName;
            var householdKey = Household.MakeHouseholdKey(h.ComplexName, sa.Standort, sa.BusinessPartnerName);
            string hausanschlussGuid = hausanschlusses.Single(x => x.Isn == sa.IsnID).HausanschlussGuid;
            var hh = new PotentialHousehold(houseGuid,householdGuid,yearlyElectricityUse,tarif,
            numberOfLocalnetEntries,businessPartnerName, householdKey, hausanschlussGuid);

            hh.Standorte.Add(sa.Standort);
            hh.LocalnetEntries = sa.LowVoltageElectricityEntries;
            if (sa.HighVoltageTotalElectricity > 0) {
                throw new Exception("Haus mit MS");
            }
            return hh;
        }

        private class StandortAnalysis {
            public StandortAnalysis([ItemNotNull] [NotNull] List<Localnet> localNetEntries, [NotNull] Logger logger, [NotNull] string standort, [NotNull] string geschäftspartner,
                                    [NotNull] Database dbHouse, [ItemNotNull] [NotNull] List<BusinessName> businesses,
                                    [ItemNotNull] [NotNull] List<string> alreadyRegisteredPotentialBusinesses)
            {
                Standort = standort;
                var isns = localNetEntries.Select(x => x.StandortID).Distinct().ToList();
                isns.Remove(-2146826246);
                if (isns.Count> 1) {
                    throw new FlaException("More than one isn on a single standort");
                }

                if (isns.Count == 1) {
                    IsnID = isns[0];
                }
                else {
                    IsnID = null;
                }


                LocalNetEntries = localNetEntries;
                BusinessPartnerName = geschäftspartner;
                RechnungsArtList = localNetEntries.Select(x => x.Rechnungsart).Distinct().ToList();
                Rechnungsart = RechnungsArtList[0];
                if (RechnungsArtList.Count != 1) {
                    var rechnungsarten = MergeStringList(RechnungsArtList);
                    logger.AddMessage(new LogMessage(MessageType.Warning, "More than one rechnungsart for a single standort: " + rechnungsarten, "A1Hhouseholds", Stage.Houses, null));
                }

                LowVoltageElectricityDuringDayList = localNetEntries.Where(x => x.Verrechnungstyp == "Netz Tagesstrom (HT)" && x.Tarif !="MS").ToList();
                LowVoltageElectricityEntries = new List<Localnet>();
                LowVoltageElectricityEntries.AddRange(LowVoltageElectricityDuringDayList);
                // ReSharper disable once PossibleInvalidOperationException
                LowVoltageElectricityUseDaytime = (double)LowVoltageElectricityDuringDayList.Select(x => x.BasisVerbrauch).Sum();
                LowVoltageElectricityDuringNightList = localNetEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)" && x.Tarif != "MS").ToList();
                LowVoltageElectricityEntries.AddRange(LowVoltageElectricityDuringNightList);
                // ReSharper disable once PossibleInvalidOperationException
                LowVoltageElectricityUseNighttime = (double)LowVoltageElectricityDuringNightList.Select(x => x.BasisVerbrauch).Sum();
                LowVoltageTotalElectricity = LowVoltageElectricityUseDaytime + LowVoltageElectricityUseNighttime;

                HighVoltageElectricityDuringDayList = localNetEntries.Where(x => x.Verrechnungstyp == "Netz Tagesstrom (HT)" && x.Tarif == "MS").ToList();
                HighVoltageElectricityEntries = new List<Localnet>();
                HighVoltageElectricityEntries.AddRange(HighVoltageElectricityDuringDayList);
                // ReSharper disable once PossibleInvalidOperationException
                HighVoltageElectricityUseDaytime = (double)HighVoltageElectricityDuringDayList.Select(x => x.BasisVerbrauch).Sum();
                HighVoltageElectricityDuringNightList = localNetEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)" && x.Tarif == "MS").ToList();
                HighVoltageElectricityEntries.AddRange(HighVoltageElectricityDuringNightList);
                // ReSharper disable once PossibleInvalidOperationException
                HighVoltageElectricityUseNighttime = (double)HighVoltageElectricityDuringNightList.Select(x => x.BasisVerbrauch).Sum();
                HighVoltageTotalElectricity = HighVoltageElectricityUseDaytime + HighVoltageElectricityUseNighttime;
                //electricity tarif
                var tarif1 = LowVoltageElectricityDuringDayList.Select(x => x.Tarif).Distinct().ToList();
                tarif1.AddRange(LowVoltageElectricityDuringDayList.Select(x => x.Tarif).Distinct());
                ElectricityTarif = MergeStringList(tarif1.Distinct().ToList());

                GasUseList = localNetEntries.Where(x => x.Verrechnungstyp == "Erdgasverbrauch").ToList();
                // ReSharper disable once PossibleInvalidOperationException
                GasUse = (double)GasUseList.Select(x => x.BasisVerbrauch).Sum();
                FernWärmeList = localNetEntries.Where(x => x.Verrechnungstyp == "Arbeitspreis").ToList();
                // ReSharper disable once PossibleInvalidOperationException
                FernWärme = (double)FernWärmeList.Select(x => x.BasisVerbrauch).Sum();
                BusinessCategory = GetGeschäftspartnerCategory(businesses, geschäftspartner, dbHouse,
                    alreadyRegisteredPotentialBusinesses, LowVoltageTotalElectricity+HighVoltageTotalElectricity, GasUse, FernWärme);
                //todo: figure out sumemr base gas use and summer base electricity use
            }

            [CanBeNull]
            public int? IsnID { get; }

            [CanBeNull]
            public string BusinessCategory { get; }

            [NotNull]
            public string BusinessPartnerName { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> LowVoltageElectricityDuringDayList { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> LowVoltageElectricityDuringNightList { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> HighVoltageElectricityDuringDayList { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> HighVoltageElectricityDuringNightList { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> LowVoltageElectricityEntries { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> HighVoltageElectricityEntries { get; }

            [NotNull]
            public string ElectricityTarif { get; }

            public double LowVoltageElectricityUseDaytime { get; }
            public double LowVoltageElectricityUseNighttime { get; }

            public double HighVoltageElectricityUseDaytime { get; }
            public double HighVoltageElectricityUseNighttime { get; }
            public double FernWärme { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> FernWärmeList { get; }

            public double GasUse { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> GasUseList { get; }

            [ItemNotNull]
            [NotNull]
            public List<Localnet> LocalNetEntries { [UsedImplicitly] get; }

            [NotNull]
            public string Rechnungsart { get; }


            [ItemNotNull]
            [NotNull]
            public List<string> RechnungsArtList { get; }

            [NotNull]
            public string Standort { get; }

            public double LowVoltageTotalElectricity { get; }
            public double HighVoltageTotalElectricity { get; }
            public double SummerLowVoltageBaseElectricity { get; [UsedImplicitly] set; }
            [UsedImplicitly]
            public double SummerBaseGasUse { get; set; }

            public bool IsBusiness()
            {
                if (!string.IsNullOrWhiteSpace(BusinessCategory)) {
                    return true;
                }

                if (Rechnungsart == "Industrie") {
                    return true;
                }

                if (ElectricityTarif.Contains("KMU")) {
                    return true;
                }

                return false;
            }

            [NotNull]
            private string MergeStringList([ItemNotNull] [NotNull] List<string> list)
            {
                var sb = new StringBuilder();
                foreach (var s in list) {
                    sb.Append(s).Append(", ");
                }

                if (sb.ToString().EndsWith(", ")) {
                    sb.Remove(sb.Length - 2, 2);
                }

                return sb.ToString();
            }
        }
    }
}