using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using ServiceRepository = BurgdorfStatistics.Tooling.ServiceRepository;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class G_DHWSystemAssignment : RunableWithBenchmark {
        public G_DHWSystemAssignment([NotNull] ServiceRepository services)
            : base(nameof(G_DHWSystemAssignment), Stage.Houses, 800, services, false)
        {
            DevelopmentStatus.Add("//todo: change the heating systeme electricity to only use the lower tarif electricity at night");
            DevelopmentStatus.Add("fill in the missing dhw heating system types");
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<DHWHeaterEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouses.Fetch<House>();
            var houseHeatings = dbHouses.Fetch<HouseHeating>();
            if (houseHeatings.All(x => x.KantonDhwMethods.Count == 0)) {
                throw new Exception("not a single  dhw heating method was set");
            }

            if (houseHeatings.All(x => x.KantonHeatingMethods.Count == 0)) {
                throw new Exception("not a single  space heating method was set");
            }

            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                var hausanschluss = house.Hausanschluss[0];
                var dhwHeaterEntry = new DHWHeaterEntry(house.HouseGuid, Guid.NewGuid().ToString(),hausanschluss.HausanschlussGuid,house.ComplexName);
                var houseHeating = houseHeatings.Single(x => x.HouseGuid == house.HouseGuid);
                var heatingMethod = HeatingSystemType.Unbekannt;
                if (houseHeating.KantonDhwMethods.Count > 0) {
                    heatingMethod = houseHeating.KantonDhwMethods[0];
                }

                if (Constants.ScrambledEquals(houseHeating.KantonHeatingMethods, houseHeating.KantonDhwMethods)) {
                    dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.IntegratedInHeating;
                }
                else {
                    switch (heatingMethod) {
                        case HeatingSystemType.Electricity:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Electricity;
                            // electricity at night
                            break;
                        case HeatingSystemType.SolarThermal:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Sonne;
                            break;
                        case HeatingSystemType.Gas:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Gasheating;
                            break;
                        case HeatingSystemType.Heatpump:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Heatpump;
                            break;
                        case HeatingSystemType.Fernwärme:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.DistrictHeating;
                            break;
                        case HeatingSystemType.Other:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Other;
                            break;
                        case HeatingSystemType.None:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Unknown;
                            break;
                        case HeatingSystemType.Öl:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.OilHeating;
                            break;
                        case HeatingSystemType.Holz:
                            dhwHeaterEntry.DhwHeatingSystemType = DhwHeatingSystem.Wood;
                            break;
                        case HeatingSystemType.Unbekannt:
                            break;
                        case HeatingSystemType.GasheatingLocalnet:
                            break;
                        case HeatingSystemType.FernwärmeLocalnet:
                            break;
                        case HeatingSystemType.FeuerungsstättenOil:
                            break;
                        case HeatingSystemType.FeuerungsstättenGas:
                            break;
                        case HeatingSystemType.Kohle:
                            break;
                        default: throw new Exception("Unknown heating method: " + heatingMethod);
                    }
                }

                dbHouses.Save(dhwHeaterEntry);
            }

            dbHouses.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var dhwHeaterEntries = dbHouse.Fetch<DHWHeaterEntry>();
            MakeDhwHeatingSystemSankey();
            HeatingSystemCountHistogram();
            MakeHeatingSystemMap();

            void MakeDhwHeatingSystemSankey()
            {
                var ssa = new SingleSankeyArrow("HouseDhwHeatingSystems", 1500, MyStage,
                    SequenceNumber, Name, Services.Logger, slice);
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

                var filename = MakeAndRegisterFullFilename("DhwHeatingSystemHistogram.png", Name, "", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                var column = 0;
                foreach (var pair in counts) {
                    names.Add(pair.Value.ToString());
                    var count = pair.Value;
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(pair.Key.ToString(), count, column));
                    column++;
                }

                Services.PlotMaker.MakeBarChart(filename, "", barSeries, names);
            }

            void MakeHeatingSystemMap()
            {
                var rgbs = new Dictionary<DhwHeatingSystem, RGB>();
                var hs = dhwHeaterEntries.Select(x => x.DhwHeatingSystemType).Distinct().ToList();
                var cg = new ColorGenerator();
                var idx = 0;
                foreach (var type in hs) {
                    rgbs.Add(type, cg.GetRGB(idx++));
                }

                RGB GetColor(House h)
                {
                    var hse = dhwHeaterEntries.Single(x => x.HouseGuid == h.HouseGuid);
                    return rgbs[hse.DhwHeatingSystemType];
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("DhwHeatingSystemMap.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in rgbs) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }
    }
}