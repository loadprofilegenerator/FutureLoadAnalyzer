using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.Database;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using ServiceRepository = FutureLoadAnalyzerLib.Tooling.ServiceRepository;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class G_DHWSystemAssignment : RunableWithBenchmark {
        public G_DHWSystemAssignment([NotNull] ServiceRepository services) : base(nameof(G_DHWSystemAssignment), Stage.Houses, 800, services, false)
        {
            DevelopmentStatus.Add("//todo: change the heating systeme electricity to only use the lower tarif electricity at night");
            DevelopmentStatus.Add("fill in the missing dhw heating system types");
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<DHWHeaterEntry>();
            var houses = dbHouses.Fetch<House>();
            var households = dbHouses.Fetch<Household>();
            var houseHeatings = dbHouses.Fetch<HouseHeating>();
            var hausanschlusses = dbHouses.Fetch<Hausanschluss>();
            if (houseHeatings.All(x => x.KantonDhwMethods.Count == 0)) {
                throw new Exception("not a single  dhw heating method was set");
            }

            if (houseHeatings.All(x => x.KantonHeatingMethods.Count == 0)) {
                throw new Exception("not a single  space heating method was set");
            }

            RowCollection rc = new RowCollection("Sheet1", "Sheet1");
            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                var householdInHouse = households.Where(x => x.HouseGuid == house.Guid).ToList();
                var occupantsInHouse = householdInHouse.SelectMany(x => x.Occupants).ToList();
                var dhwHeaterEntry = new DHWHeaterEntry(house.Guid, Guid.NewGuid().ToString(), "DHW@" + house.ComplexName);
                var houseHeating = houseHeatings.Single(x => x.HouseGuid == house.Guid);
                var heatingMethod = HeatingSystemType.Unbekannt;
                if (houseHeating.KantonDhwMethods.Count > 0) {
                    heatingMethod = houseHeating.GetDominantDhwHeatingMethod();
                }

                var peopleInHouse = occupantsInHouse.Count;
                RowBuilder rb = RowBuilder.Start("House", house.ComplexName);
                rc.Add(rb);
                rb.Add("Households", households.Count);
                rb.Add("Persons", occupantsInHouse.Count);

                rb.Add("Ebbe DHW Estimate", houseHeating.KantonWarmwasserEnergyDemand);
                switch (heatingMethod) {
                    case HeatingSystemType.Electricity:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                        // electricity at night
                        break;
                    case HeatingSystemType.SolarThermal:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                        break;
                    case HeatingSystemType.Gas:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Gasheating;
                        break;
                    case HeatingSystemType.Heatpump:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                        break;
                    case HeatingSystemType.Fernwärme:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.DistrictHeating;
                        break;
                    case HeatingSystemType.Other:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                        break;
                    case HeatingSystemType.None:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.None;
                        break;
                    case HeatingSystemType.Öl:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.OilHeating;
                        break;
                    case HeatingSystemType.Holz:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                        break;
                    case HeatingSystemType.Unbekannt:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                        break;
                    case HeatingSystemType.GasheatingLocalnet:
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Gasheating;
                        break;
                    case HeatingSystemType.FernwärmeLocalnet:
                        throw new Exception("Unknown heating method: " + heatingMethod);
                    case HeatingSystemType.FeuerungsstättenOil:
                        throw new Exception("Unknown heating method: " + heatingMethod);
                    case HeatingSystemType.FeuerungsstättenGas:
                        throw new Exception("Unknown heating method: " + heatingMethod);
                    case HeatingSystemType.Kohle:
                        throw new Exception("Unknown heating method: " + heatingMethod);
                    default: throw new Exception("Unknown heating method: " + heatingMethod);
                }

                rb.Add("Heating Method", dhwHeaterEntry.DhwHeatingSystemType);

                double totalEnergy = 0;
                double dhwEnergy = 0;
                Hausanschluss hausanschluss;
                string hausanschlussGuid = null;
                string standort = null;
                foreach (var hh in householdInHouse) {
                    double personEnergy = hh.EffectiveEnergyDemand / hh.Occupants.Count;
                    if (personEnergy > 1800) {
                        dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                    }
                }

                foreach (var hh in householdInHouse) {
                    double hhdhwEnergy = 0;
                    double personEnergy = hh.EffectiveEnergyDemand / hh.Occupants.Count;

                    if (dhwHeaterEntry.DhwHeatingSystemType == DhwHeatingSystem.Electricity) {
                        hhdhwEnergy = CalculateDHWEnergy(personEnergy) * hh.Occupants.Count;
                    }

                    var rbhh = RowBuilder.Start("household", hh.Name);
                    rbhh.Add("Persons", hh.Occupants.Count);
                    rbhh.Add("Energy Per Person in Household [kWh]", hhdhwEnergy);
                    rbhh.Add("Household DHW heating method", dhwHeaterEntry.DhwHeatingSystemType);
                    rbhh.Add("Household Energy", hh.EffectiveEnergyDemand);
                    rc.Add(rbhh);
                    if (Math.Abs(hh.EffectiveEnergyDemand - hh.LocalnetLowVoltageYearlyTotalElectricityUse) > 0.1) {
                        if (Math.Abs(hh.EffectiveEnergyDemand - (hh.LocalnetLowVoltageYearlyTotalElectricityUse + dhwEnergy)) > 0.1) {
                            throw new FlaException("Energy use does not fit");
                        }
                    }

                    hh.SetEnergyReduction("DHW", hhdhwEnergy);
                    if (hh.EffectiveEnergyDemand < 0) {
                        throw new FlaException("Effective Energy demand was null");
                    }

                    totalEnergy += hh.EffectiveEnergyDemand;
                    hausanschlussGuid = hh.HausAnschlussGuid;
                    standort = hh.Standort;
                    dbHouses.Save(hh);
                    dhwEnergy += hhdhwEnergy;
                }

                if (hausanschlussGuid != null) {
                    hausanschluss = hausanschlusses.Single(x => x.Guid == hausanschlussGuid);
                }
                else {
                    hausanschluss = house.GetHausanschlussByIsn(new List<int>(), null, hausanschlusses, MyLogger) ??
                                    throw new FlaException("no hausanschluss");
                }

                dhwHeaterEntry.Standort = standort;
                dhwHeaterEntry.HausAnschlussGuid = hausanschluss.Guid;
                dhwHeaterEntry.EffectiveEnergyDemand = dhwEnergy;
                if (totalEnergy < 0) {
                    throw new FlaException("Negative total energy");
                }

                rb.Add("Total Energy Originally [kWh]", totalEnergy);
                rb.Add("Total Energy DHW [kWh]", dhwEnergy);
                rb.Add("DHW Power [kWh]", dhwEnergy / 365 / 2);
                rb.Add("Total Energy After Dhw [kWh]", totalEnergy - dhwEnergy);
                rb.Add("Energy Per Person [kWh]", totalEnergy / peopleInHouse);

                dbHouses.Save(dhwHeaterEntry);
            }

            dbHouses.CompleteTransaction();
            var hhdump = MakeAndRegisterFullFilename("Households.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(hhdump, rc);
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var dhwHeaterEntries = dbHouse.Fetch<DHWHeaterEntry>();
            MakeDhwHeatingSystemSankey();
            HeatingSystemCountHistogram();
            MakeHeatingSystemMap();

            void MakeDhwHeatingSystemSankey()
            {
                var ssa = new SingleSankeyArrow("HouseDhwHeatingSystems", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var counts = new Dictionary<DhwHeatingSystem, int>();
                foreach (var entry in dhwHeaterEntries) {
                    if (!counts.ContainsKey(entry.DhwHeatingSystemType)) {
                        counts.Add(entry.DhwHeatingSystemType, 0);
                    }

                    counts[entry.DhwHeatingSystemType]++;
                }

                var i = 1;
                foreach (var pair in counts) {
                    ssa.AddEntry(new SankeyEntry(pair.Key.ToString(), pair.Value * -1, 2000 * i, Orientation.Up));
                    i++;
                }

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void HeatingSystemCountHistogram()
            {
                var counts = new Dictionary<DhwHeatingSystem, int>();
                foreach (var entry in dhwHeaterEntries) {
                    if (!counts.ContainsKey(entry.DhwHeatingSystemType)) {
                        counts.Add(entry.DhwHeatingSystemType, 0);
                    }

                    counts[entry.DhwHeatingSystemType]++;
                }

                var filename = MakeAndRegisterFullFilename("DhwHeatingSystemHistogram.png", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                var column = 0;
                foreach (var pair in counts) {
                    names.Add(pair.Value.ToString());
                    var count = pair.Value;
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(pair.Key.ToString(), count, column));
                    column++;
                }

                Services.PlotMaker.MakeBarChart(filename, "DhwHeatingSystemHistogram", barSeries, names);
            }

            void MakeHeatingSystemMap()
            {
                var rgbs = new Dictionary<DhwHeatingSystem, RGB>();
                var hs = dhwHeaterEntries.Select(x => x.DhwHeatingSystemType).Distinct().ToList();
                var idx = 0;
                foreach (var type in hs) {
                    rgbs.Add(type, ColorGenerator.GetRGB(idx++));
                }

                RGB GetColor(House h)
                {
                    var hse = dhwHeaterEntries.Single(x => x.HouseGuid == h.Guid);
                    return rgbs[hse.DhwHeatingSystemType];
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("DhwHeatingSystemMap.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in rgbs) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }

        private static double CalculateDHWEnergy(double personEnergy)
        {
            if (personEnergy < 700) {
                return 0;
            }

            double dhwEnergy = personEnergy - 500;
            if (dhwEnergy > 700) {
                return 700;
            }

            return dhwEnergy;
        }
    }
}