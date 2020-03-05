using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class L_AssignAirConditioning : RunableWithBenchmark {
        public L_AssignAirConditioning([NotNull] ServiceRepository services) : base(nameof(L_AssignAirConditioning),
            Stage.Houses,
            1200,
            services,
            false)
        {
            DevelopmentStatus.Add("factor floor area into air conditioning load //todo: factor in floor area");
            DevelopmentStatus.Add("Add //todo: put in industrial air conditionings");
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<AirConditioningEntry>();
            var houses = dbHouses.Fetch<House>();
            var businesses = dbHouses.Fetch<BusinessEntry>();
            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                var housebusinesses = businesses.Where(x => x.HouseGuid == house.Guid);
                foreach (var business in housebusinesses) {
                    if (business.Name.Contains("ML & G AG, Migrol Service M. Leibundgut, Kirchbergstrasse 235, 3400 Burgdorf")) {
                        Console.WriteLine("hi");
                    }

                    if (business.BusinessType == BusinessType.Shop && business.EffectiveEnergyDemand > 20000 &&
                        string.IsNullOrWhiteSpace(business.RlmProfileName)) {
                        var airconditioningEntry = new AirConditioningEntry(house.Guid,
                            Guid.NewGuid().ToString(),
                            business.EffectiveEnergyDemand * 0.1,
                            3,
                            AirConditioningType.Commercial,
                            business.HausAnschlussGuid,
                            house.ComplexName + " - Air Conditioning",
                            business.Standort);
                        business.SetEnergyReduction("AC", airconditioningEntry.EffectiveEnergyDemand);
                        dbHouses.Save(airconditioningEntry);
                        dbHouses.Save(business);
                    }

                    if (business.BusinessType == BusinessType.Industrie && business.EffectiveEnergyDemand > 200000 &&
                        string.IsNullOrWhiteSpace(business.RlmProfileName)) {
                        var airconditioningEntry = new AirConditioningEntry(house.Guid,
                            Guid.NewGuid().ToString(),
                            business.EffectiveEnergyDemand * 0.1,
                            3,
                            AirConditioningType.Industrial,
                            business.HausAnschlussGuid,
                            house.ComplexName + " - Air conditioning",
                            business.Standort);
                        business.SetEnergyReduction("AC", airconditioningEntry.EffectiveEnergyDemand);
                        dbHouses.Save(airconditioningEntry);
                        dbHouses.Save(business);
                    }
                }
            }

            dbHouses.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var airconditioing = dbHouse.Fetch<AirConditioningEntry>();
            var airConditioningGuids = airconditioing.Select(x => x.HouseGuid).Distinct().ToList();
            MakeAirConditioningSankeySankey();
            MakeAirConditioningMap();

            void MakeAirConditioningSankeySankey()
            {
                var ssa = new SingleSankeyArrow("AirConditioingHouses", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var housesWithLight = airConditioningGuids.Count;

                ssa.AddEntry(new SankeyEntry("Klimatisierung", housesWithLight * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Sonstige Häuser", (houses.Count - housesWithLight) * -1, 2000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeAirConditioningMap()
            {
                RGBWithSize GetColor(House h)
                {
                    var ace = airconditioing.FirstOrDefault(x => x.HouseGuid == h.Guid);
                    if (ace == null) {
                        return new RGBWithSize(Constants.Black, 10);
                    }

                    if (ace.AirConditioningType == AirConditioningType.Commercial) {
                        return new RGBWithSize(Constants.Red, 20);
                    }

                    if (ace.AirConditioningType == AirConditioningType.Industrial) {
                        return new RGBWithSize(Constants.Blue, 20);
                    }

                    throw new Exception("Unbekannte Klimatisierung");
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor, House.CoordsToUse.Localnet)).ToList();

                var filename = MakeAndRegisterFullFilename("Klimatisierung.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Klimatisierung (GHD)", Constants.Red),
                    new MapLegendEntry("Klimatisierung (Industrie)", Constants.Blue),
                    new MapLegendEntry("Keine Klimatisierung", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}