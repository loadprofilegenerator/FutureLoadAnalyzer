using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace BurgdorfStatistics._04_HouseMaker {

    public enum EnergyDemandSource {
        LocalNet,
        Kanton,
        Other
    }
    public class OverrideEntry {
        [NotNull]
        public string ComplexName { get;  }
        public HeatingSystemType HeatingSystemType { get;  }
        public EnergyDemandSource Source { get;  }

        public OverrideEntry([NotNull] string complexName, HeatingSystemType heatingSystemType, EnergyDemandSource source, double amount)
        {
            ComplexName = complexName;
            HeatingSystemType = heatingSystemType;
            Source = source;
            Amount = amount;
        }

        public double Amount { get; }
    }

    public class OverrideRepository {
        [NotNull]
        [ItemNotNull]
        public List<OverrideEntry> ReadEntries()
        {
            string path = @"V:\Dropbox\BurgdorfStatistics\Corrections\Corrections.xlsx";
            var p = new ExcelPackage(new FileInfo(path));
            var ws = p.Workbook.Worksheets[1];
            int row = 2;
            List<OverrideEntry> ores = new List<OverrideEntry>();
            while (ws.Cells[row, 1].Value != null) {
                string name = (string)ws.Cells[row, 1].Value;
                string heatingSystemTypeStr = (string)ws.Cells[row, 2].Value;
                HeatingSystemType hst = (HeatingSystemType)Enum.Parse(typeof(HeatingSystemType), heatingSystemTypeStr);
                string energyDemandSourceStr = (string)ws.Cells[row, 3].Value;
                EnergyDemandSource eds = (EnergyDemandSource)Enum.Parse(typeof(EnergyDemandSource), energyDemandSourceStr);
                double amount = (double)ws.Cells[row, 4].Value;
                OverrideEntry ore = new OverrideEntry(name,hst,eds,amount);
                ores.Add(ore);
                row = row + 1;
            }
            p.Dispose();
            return ores;
        }
    }

    //
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// this class decides based on all available information what heating system actually applies
    /// </summary>
    public class F_AssignHeatingSystems : RunableWithBenchmark {
        public F_AssignHeatingSystems([NotNull] ServiceRepository services)
            : base(nameof(F_AssignHeatingSystems), Stage.Houses, 600, services,
                true, new HeatingSystemCharts())
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<HeatingSystemEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var rnd = new Random();
            var houses = dbHouses.Fetch<House>();

            var houseHeatingMethods = dbHouses.Fetch<HouseHeating>();
            var feuerungsstättenRaw = dbRaw.Fetch<FeuerungsStaette>();
            var complexes = dbComplexes.Fetch<BuildingComplex>();
            Dictionary<string, BuildingComplex> buildingComplexesByName = new Dictionary<string, BuildingComplex>();
            foreach (BuildingComplex complex in complexes) {
                buildingComplexesByName.Add(complex.ComplexName,complex);
            }
            var potentialHeatingSystems = dbHouses.Fetch<PotentialHeatingSystemEntry>();
            dbHouses.BeginTransaction();
            OverrideRepository overrideRepository = new OverrideRepository();
            double totalFernwärme = potentialHeatingSystems.Sum(x => x.YearlyFernwärmeDemand);
            var overrideEntries = overrideRepository.ReadEntries();
            var houseNames = houses.Select(x => x.ComplexName).ToHashSet();
            foreach (OverrideEntry entry in overrideEntries) {
                if (!houseNames.Contains(entry.ComplexName)) {
                    throw new FlaException("Kein Haus für Override Entry " + entry.ComplexName + ". Vermutlich vertippt?");
                }
            }
            List<HeatingSystemEntry> hses = new List<HeatingSystemEntry>();
            foreach (var house in houses) {
                var complex = buildingComplexesByName[house.ComplexName];
                var feuerungsStättenForHouse = feuerungsstättenRaw.Where(x => {
                    Debug.Assert(x.EGID != null, "x.EGID != null");
                    return complex.EGids.Contains((long)x.EGID);
                }).ToList();
                //set the age of the heating system, randomly up to 30 years old


                var feuerungsstättenType = string.Join(",", feuerungsStättenForHouse.Select(x => x.Brennstoff?.ToString()));
                var hausanschluss = house.Hausanschluss[0];
                var hse = new HeatingSystemEntry(house.HouseGuid, Guid.NewGuid().ToString(),feuerungsstättenType,
                    hausanschluss.HausanschlussGuid,hausanschluss.Adress);

                var oldestYear = feuerungsStättenForHouse.Min(x => x.KesselBaujahr);
                if (oldestYear == null)
                {
                    hse.Age = rnd.Next(30);
                }
                else
                {
                    hse.Age = 2019 - oldestYear.Value;
                }

                hse.FeuerungsstättenPower = feuerungsStättenForHouse.Sum(x=> {
                    if (x.KesselLeistung == null) {
                        throw new FlaException("Kesselleistung was null");
                    }
                    return (double)x.KesselLeistung;
                });
                hse.EstimatedMinimumEnergyFromFeuerungsStätten = hse.FeuerungsstättenPower * 1500;
                hse.EstimatedMaximumEnergyFromFeuerungsStätten = hse.FeuerungsstättenPower * 2200;
                //this is a localnet heating system, so use the localnet information
                var potentialHouseHeatingSystems = potentialHeatingSystems.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                var houseHeatingMethod = houseHeatingMethods.Single(x => x.HouseGuid == house.HouseGuid);
                hse.AverageHeatingEnergyDemandDensity = houseHeatingMethod.MergedHeatingEnergyDensity;
                OverrideEntry ore = overrideEntries.FirstOrDefault(x => x.ComplexName == house.ComplexName);
                if (ore != null) {
                    Info("Override Entry for " + house.ComplexName);
                }
                if (potentialHouseHeatingSystems.Count > 0) {
                    var totalGas = potentialHouseHeatingSystems.Sum(x => x.YearlyGasDemand);
                    var totalWärme = potentialHouseHeatingSystems.Sum(x => x.YearlyFernwärmeDemand);
                    if (totalGas > 0) {
                        //localnet heizung
                        hse.YearlyEnergyDemand = totalGas;
                        hse.OriginalHeatingSystemType = HeatingSystemType.GasheatingLocalnet;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Gas;
                        if (totalWärme > 0) {
                            throw new Exception("Both wärme and gas");
                        }

                        if (ore != null) {
                            throw new FlaException("Overrride Entry für Localnet Gas Gebäude: " + house.ComplexName + ", but that doesn't make sense");
                        }
                    }

                    if (totalWärme > 0) {
                        hse.YearlyEnergyDemand = totalWärme;
                        hse.OriginalHeatingSystemType = HeatingSystemType.FernwärmeLocalnet;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Fernwärme;
                        if (ore != null)
                        {
                            throw new FlaException("Overrride Entry für Localnet Gebäude: " + house.ComplexName);
                        }
                    }
                } // no data, so use feuerungsstätten
                else if (ore != null)
                {
                    //kanton heizung
                    hse.OriginalHeatingSystemType = HeatingSystemType.None;
                    hse.SynthesizedHeatingSystemType = ore.HeatingSystemType;
                    hse.YearlyEnergyDemand = ore.Amount;

                }
                else if (feuerungsStättenForHouse.Count > 0) {
                    var fs = feuerungsStättenForHouse[0];
                    if (fs.Brennstoff == "Oel") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenOil;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        hse.YearlyEnergyDemand = houseHeatingMethod.KantonTotalEnergyDemand;

                    }
                    else if (fs.Brennstoff == "Gas") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenGas;
                        //beco says gas, but can't be, because localnet says no
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        hse.YearlyEnergyDemand = houseHeatingMethod.KantonTotalEnergyDemand;
                    }
                    else {
                        throw new Exception("invalid heating system");
                    }
                } // no beco, so use ebbe daten
                else if (houseHeatingMethod.KantonHeatingMethods.Count > 0) {
                    //kanton heizung
                    var kantonHeatingMethod = houseHeatingMethod.KantonHeatingMethods[0];
                    GetKantonHeatingSystem(kantonHeatingMethod, hse, houseHeatingMethod);
                }else if (feuerungsStättenForHouse.Count > 0) {
                    var fs = feuerungsStättenForHouse[0];
                    if (fs.Brennstoff == "Oel") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenOil;
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        hse.YearlyEnergyDemand = houseHeatingMethod.KantonTotalEnergyDemand;

                    }
                    else if (fs.Brennstoff == "Gas") {
                        hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenGas;
                        //beco says gas, but can't be, because localnet says no
                        hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                        hse.YearlyEnergyDemand = houseHeatingMethod.KantonTotalEnergyDemand;
                    }
                    else {
                        throw new Exception("invalid heating system");
                    }
                } // no beco, so use ebbe daten
                else if (houseHeatingMethod.KantonHeatingMethods.Count > 0) {
                    //kanton heizung
                    var kantonHeatingMethod = houseHeatingMethod.KantonHeatingMethods[0];
                    GetKantonHeatingSystem(kantonHeatingMethod, hse, houseHeatingMethod);
                }
                else {
                    hse.OriginalHeatingSystemType = HeatingSystemType.None;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.None;
                    hse.YearlyEnergyDemand = 0;
                }


                dbHouses.Save(hse);
                hses.Add(hse);
            }
            dbHouses.CompleteTransaction();
            double finalFernwärme = hses.Where(x => x.SynthesizedHeatingSystemType == HeatingSystemType.Fernwärme).Sum(x => x.YearlyEnergyDemand);
            if (Math.Abs(finalFernwärme - totalFernwärme) > 1) {
                throw new FlaException("Fernwärme changed: Nach allem:" + finalFernwärme  + " davor: " + totalFernwärme);
            }
        }


        private static void GetKantonHeatingSystem(HeatingSystemType kantonHeatingMethod, [NotNull] HeatingSystemEntry hse, [NotNull] HouseHeating houseHeatingData)
        {
            switch (kantonHeatingMethod) {
                case HeatingSystemType.Electricity:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Electricity;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Electricity;
                    // change this to consider the canton values and leave some electricty rest
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Heatpump:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Heatpump;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Heatpump;
                    // change this to consider the canton values and leave some electricty rest
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.SolarThermal:
                    hse.OriginalHeatingSystemType = HeatingSystemType.SolarThermal;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Gas:
                    //can't be gas, since localnet says it is not gas
                    hse.OriginalHeatingSystemType = HeatingSystemType.Gas;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Fernwärme:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Fernwärme;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Öl:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Öl;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    //: change this to consider the canton values and leave some electricty rest
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Kohle:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Kohle;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
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
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Holz:
                    hse.OriginalHeatingSystemType = HeatingSystemType.Holz;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Other;
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    break;
                case HeatingSystemType.Unbekannt:
                    break;
                case HeatingSystemType.GasheatingLocalnet:
                    hse.OriginalHeatingSystemType = HeatingSystemType.GasheatingLocalnet;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Gas;
                    hse.YearlyEnergyDemand = houseHeatingData.LocalnetGasEnergyUse;
                    break;
                case HeatingSystemType.FernwärmeLocalnet:
                    hse.OriginalHeatingSystemType = HeatingSystemType.FernwärmeLocalnet;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Fernwärme;
                    hse.YearlyEnergyDemand = houseHeatingData.LocalnetFernwärmeEnergyUse;
                    break;
                case HeatingSystemType.FeuerungsstättenOil:
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenOil;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    break;
                case HeatingSystemType.FeuerungsstättenGas:
                    hse.YearlyEnergyDemand = houseHeatingData.KantonTotalEnergyDemand;
                    hse.OriginalHeatingSystemType = HeatingSystemType.FeuerungsstättenGas;
                    hse.SynthesizedHeatingSystemType = HeatingSystemType.Öl;
                    break;
                default: throw new Exception("Unknown heating method: " + kantonHeatingMethod);
            }
        }
    }
}