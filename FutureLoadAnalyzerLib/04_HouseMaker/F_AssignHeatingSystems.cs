using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public enum EnergyDemandSource {
        LocalNet,
        Kanton,
        Other
    }

    // ReSharper disable once InconsistentNaming
    /// <summary>
    ///     this class decides based on all available information what heating system actually applies
    /// </summary>
    public class F_AssignHeatingSystems : RunableWithBenchmark {
        public F_AssignHeatingSystems([NotNull] ServiceRepository services) : base(nameof(F_AssignHeatingSystems),
            Stage.Houses,
            600,
            services,
            true,
            new HeatingSystemCharts(services, Stage.Houses))
        {
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<HeatingSystemEntry>();
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbComplexes = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var rnd = new Random();
            var houses = dbHouses.Fetch<House>();
            var hausanschlusses = dbHouses.Fetch<Hausanschluss>();
            var houseHeatingMethods = dbHouses.Fetch<HouseHeating>();
            var feuerungsstättenRaw = dbRaw.Fetch<FeuerungsStaette>();
            var complexes = dbComplexes.Fetch<BuildingComplex>();
            Dictionary<string, BuildingComplex> buildingComplexesByName = new Dictionary<string, BuildingComplex>();
            foreach (BuildingComplex complex in complexes) {
                buildingComplexesByName.Add(complex.ComplexName, complex);
            }

            var potentialHeatingSystems = dbHouses.Fetch<PotentialHeatingSystemEntry>();
            dbHouses.BeginTransaction();
            OverrideRepository overrideRepository = new OverrideRepository();
            double totalFernwärme = potentialHeatingSystems.Sum(x => x.YearlyFernwärmeDemand);
            var overrideEntries = overrideRepository.ReadEntries(Services);
            var houseNames = houses.Select(x => x.ComplexName).ToHashSet();
            foreach (OverrideEntry entry in overrideEntries) {
                if (!houseNames.Contains(entry.ComplexName)) {
                    throw new FlaException("Kein Haus für Override Entry " + entry.ComplexName + ". Vermutlich vertippt?");
                }
            }

            List<HeatingSystemEntry> hses = new List<HeatingSystemEntry>();
            foreach (var house in houses) {
                var complex = buildingComplexesByName[house.ComplexName];
                List<FeuerungsStaette> feuerungsStättenForHouse = feuerungsstättenRaw.Where(x => {
                    if (x.EGID == null) {
                        throw new FlaException("x.EGID != null");
                    }

                    return complex.EGids.Contains((long)x.EGID);
                }).ToList();

                //set the age of the heating system, randomly up to 30 years old
                var feuerungsstättenType = string.Join(",", feuerungsStättenForHouse.Select(x => x.Brennstoff?.ToString()).Distinct());
                var hausanschluss = house.GetHausanschlussByIsn(new List<int>(), null, hausanschlusses, MyLogger, false);
                if (hausanschluss != null && hausanschluss.ObjectID.ToLower().Contains("kleinanschluss")) {
                    hausanschluss = null;
                }

                var hse = new HeatingSystemEntry(house.Guid,
                    Guid.NewGuid().ToString(),
                    feuerungsstättenType,
                    hausanschluss?.Guid,
                    house.ComplexName,
                    house.ComplexName + " - Heating System  - Unknown");
                hses.Add(hse);
                var oldestYear = feuerungsStättenForHouse.Min(x => x.KesselBaujahr);
                if (oldestYear == null || oldestYear.Value < 1800) {
                    hse.Age = rnd.Next(30);
                }
                else {
                    hse.Age = 2019 - oldestYear.Value;
                }

                hse.FeuerungsstättenPower = feuerungsStättenForHouse.Sum(x => {
                    if (x.KesselLeistung == null) {
                        throw new FlaException("Kesselleistung was null");
                    }

                    return (double)x.KesselLeistung;
                });
                hse.EstimatedMinimumEnergyFromFeuerungsStätten = hse.FeuerungsstättenPower * 1500;
                hse.EstimatedMaximumEnergyFromFeuerungsStätten = hse.FeuerungsstättenPower * 2200;
                //this is a localnet heating system, so use the localnet information
                var potentialHouseHeatingSystems = potentialHeatingSystems.Where(x => x.HouseGuid == house.Guid).ToList();
                var houseHeatingMethod = houseHeatingMethods.Single(x => x.HouseGuid == house.Guid);
                double totalHeatDemand = 0;
                OverrideEntry ore = overrideEntries.FirstOrDefault(x => x.ComplexName == house.ComplexName);
                if (ore != null) {
                    Debug("Override Entry for " + house.ComplexName);
                }

                if (potentialHouseHeatingSystems.Count > 0) {
                    var totalGas = potentialHouseHeatingSystems.Sum(x => x.YearlyGasDemand);
                    var totalWärme = potentialHouseHeatingSystems.Sum(x => x.YearlyFernwärmeDemand);
                    if (totalGas > 0) {
                        //localnet heizung
                        totalHeatDemand = totalGas;
                        hse.OriginalHeatingSystemType = HeatingSystemType.GasheatingLocalnet;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Gas;
                        if (totalWärme > 0) {
                            throw new Exception("Both wärme and gas");
                        }

                        if (ore != null) {
                            throw new FlaException("Overrride Entry für Localnet Gas Gebäude: " + house.ComplexName +
                                                   ", but that doesn't make sense");
                        }
                    }

                    if (totalWärme > 0) {
                        totalHeatDemand = totalWärme;
                        hse.OriginalHeatingSystemType = HeatingSystemType.FernwärmeLocalnet;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Fernwärme;
                    }
                } // no data, so use feuerungsstätten
                else if (ore != null) {
                    //kanton heizung
                    hse.OriginalHeatingSystemType = HeatingSystemType.None;
                    if (ore.HeatingSystemType == HeatingSystemType.Heatpump && hse.HausAnschlussGuid == null) {
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.None;
                    }

                    hse.SynthesizedHeatingSystemType = ore.HeatingSystemType;
                    totalHeatDemand = ore.Amount;
                }
                else if (feuerungsStättenForHouse.Count > 0) {
                    var fs = feuerungsStättenForHouse[0];
                    if (fs.Brennstoff == "Oel") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenOil;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        totalHeatDemand = houseHeatingMethod.KantonTotalEnergyDemand;
                    }
                    else if (fs.Brennstoff == "Gas") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenGas;
                        //beco says gas, but can't be, because localnet says no
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        totalHeatDemand = houseHeatingMethod.KantonTotalEnergyDemand;
                    }
                    else {
                        throw new Exception("invalid heating system");
                    }
                } // no beco, so use ebbe daten
                else if (houseHeatingMethod.KantonHeatingMethods.Count > 0) {
                    //kanton heizung
                    var kantonHeatingMethod = houseHeatingMethod.KantonHeatingMethods[0];
                    GetKantonHeatingSystem(kantonHeatingMethod, hse, houseHeatingMethod, ref totalHeatDemand);
                }
                else if (feuerungsStättenForHouse.Count > 0) {
                    var fs = feuerungsStättenForHouse[0];
                    if (fs.Brennstoff == "Oel") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenOil;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        totalHeatDemand = houseHeatingMethod.KantonTotalEnergyDemand;
                    }
                    else if (fs.Brennstoff == "Gas") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenGas;
                        //beco says gas, but can't be, because localnet says no
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        totalHeatDemand = houseHeatingMethod.KantonTotalEnergyDemand;
                    }
                    else {
                        throw new Exception("invalid heating system");
                    }
                } // no beco, so use ebbe daten
                else if (houseHeatingMethod.KantonHeatingMethods.Count > 0) {
                    //kanton heizung
                    var kantonHeatingMethod = houseHeatingMethod.KantonHeatingMethods[0];

                    GetKantonHeatingSystem(kantonHeatingMethod, hse, houseHeatingMethod, ref totalHeatDemand);
                }
                else {
                    hse.OriginalHeatingSystemType = HeatingSystemType.None;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.None;
                    totalHeatDemand = 0;
                }

                if (hse.SynthesizedHeatingSystemType == HeatingSystemType.Heatpump && hse.HausAnschlussGuid == null) {
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.None;
                }

                if (hse.SynthesizedHeatingSystemType == HeatingSystemType.Electricity && hse.HausAnschlussGuid == null) {
                    throw new FlaException("electric heating without anschluss");
                }

                if (hse.SynthesizedHeatingSystemType == HeatingSystemType.Heatpump && hse.HausAnschlussGuid == null) {
                    throw new FlaException("hp heating without anschluss");
                }

                hse.Standort = house.ComplexName + " - " + hse.SynthesizedHeatingSystemType;
                hse.ProvideProfile = false;
                hse.HeatingSystemType2017 = hse.SynthesizedHeatingSystemType;
                if (house.Appartments.Count == 0) {
                    throw new FlaException("Not a single area in the house " + house.ComplexName);
                }

                double avgHeatDemand = totalHeatDemand / house.Appartments.Count;
                foreach (var appartment in house.Appartments) {
                    hse.HeatDemands.Add(new AppartmentHeatingDemand(appartment.Guid,
                        appartment.EnergieBezugsFläche,
                        avgHeatDemand,
                        Constants.PresentSlice.DstYear));
                }

                if (Math.Abs(hse.HeatDemand - totalHeatDemand) > 0.01) {
                    throw new FlaException("Invalid heat demand");
                }

                hse.OriginalHeatDemand2017 = hse.HeatDemand;
                dbHouses.Save(hse);
            }

            dbHouses.CompleteTransaction();
            double finalFernwärme = hses.Where(x => x.SynthesizedHeatingSystemType == HeatingSystemType.Fernwärme).Sum(x => x.EffectiveEnergyDemand);
            if (Math.Abs(finalFernwärme - totalFernwärme) > 1) {
                throw new FlaException("Fernwärme changed: Nach allem:" + finalFernwärme + " davor: " + totalFernwärme);
            }

            RowCollection rc = new RowCollection("Validation", "Validierung");
            foreach (var feuerungsStaette in feuerungsstättenRaw) {
                string adress = feuerungsStaette.Strasse + " " + feuerungsStaette.Hausnummer;
                RowBuilder rb = RowBuilder.Start("FeuerungsAdresse", adress);
                rc.Add(rb);
                rb.Add("Brennstoff", feuerungsStaette.Brennstoff);
                rb.Add("Energienutzung", feuerungsStaette.Energienutzung);
                rb.Add("Leistung", feuerungsStaette.KesselLeistung);
                if (feuerungsStaette.EGID != null) {
                    int egid = (int)feuerungsStaette.EGID;
                    var house = houses.FirstOrDefault(x => x.EGIDs.Contains(egid));
                    if (house != null) {
                        rb.Add("Haus", house.ComplexName);
                        rb.Add("HausAdresse", house.Adress);
                        HeatingSystemEntry hse = hses.Single(x => x.HouseGuid == house.Guid);
                        rb.Add("Gewählter Heizungstyp 1", hse.OriginalHeatingSystemType.ToString());
                        rb.Add("Gewählter Heizungstyp 2", hse.SynthesizedHeatingSystemType.ToString());
                        rb.Add("EBF", house.EnergieBezugsFläche);
                    }
                    else {
                        var h2 = houses.FirstOrDefault(x => x.Adress?.Contains(adress) == true);
                        if (h2 != null) {
                            rb.Add("Findbar über Adresse", h2.ComplexName);

                            HeatingSystemEntry hse = hses.Single(x => x.HouseGuid == h2.Guid);
                            rb.Add("Gewählter Heizungstyp 1", hse.OriginalHeatingSystemType.ToString());
                            rb.Add("Gewählter Heizungstyp 2", hse.SynthesizedHeatingSystemType.ToString());
                            rb.Add("EBF", h2.EnergieBezugsFläche);
                        }
                    }
                }
                else {
                    rb.Add("Egid fehlt", "True");
                }
            }

            var fn = MakeAndRegisterFullFilename("Feuerungsstätten-Validation.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn, rc);
        }

        private static void GetKantonHeatingSystem(HeatingSystemType kantonHeatingMethod,
                                                   [NotNull] HeatingSystemEntry hse,
                                                   [NotNull] HouseHeating houseHeatingData,
                                                   ref double totalHeatDemand)
        {
            switch (kantonHeatingMethod) {
                case HeatingSystemType.Electricity:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Electricity;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Electricity;
                    // change this to consider the canton values and leave some electricty rest
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Heatpump:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Heatpump;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Heatpump;
                    // change this to consider the canton values and leave some electricty rest
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.SolarThermal:
                    hse.OriginalHeatingSystemType = HeatingSystemType.SolarThermal;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Gas:
                    //can't be gas, since localnet says it is not gas
                    hse.OriginalHeatingSystemType = HeatingSystemType.Gas;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Fernwärme:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Fernwärme;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Öl:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Öl;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    //: change this to consider the canton values and leave some electricty rest
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Kohle:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Kohle;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.None:
                    hse.OriginalHeatingSystemType = HeatingSystemType.None;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.None;
                    if (houseHeatingData.KantonTotalEnergyDemand > 1) {
                        throw new Exception("No heating method but energy demand");
                    }

                    break;
                case HeatingSystemType.Other:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Other;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Holz:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Holz;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Unbekannt:
                    break;
                case HeatingSystemType.GasheatingLocalnet:
                    hse.OriginalHeatingSystemType = HeatingSystemType.GasheatingLocalnet;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Gas;
                    totalHeatDemand = houseHeatingData.LocalnetGasEnergyUse;
                    break;
                case HeatingSystemType.FernwärmeLocalnet:
                    hse.OriginalHeatingSystemType = HeatingSystemType.FernwärmeLocalnet;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Fernwärme;
                    totalHeatDemand = houseHeatingData.LocalnetFernwärmeEnergyUse;
                    break;
                case HeatingSystemType.FeuerungsstättenOil:
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenOil;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    break;
                case HeatingSystemType.FeuerungsstättenGas:
                    totalHeatDemand = houseHeatingData.KantonTotalEnergyDemand;
                    hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenGas;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    break;
                default: throw new Exception("Unknown heating method: " + kantonHeatingMethod);
            }
        }
    }
}