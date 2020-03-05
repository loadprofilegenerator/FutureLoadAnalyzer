using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    internal class L_AssignAirConditioning : RunableWithBenchmark {
        public L_AssignAirConditioning([NotNull] ServiceRepository services)
            : base(nameof(L_AssignAirConditioning), Stage.Houses, 1200, services, false)
        {
            DevelopmentStatus.Add("factor floor area into air conditioning load //todo: factor in floor area");
            DevelopmentStatus.Add("Add //todo: put in industrial air conditionings");
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var airconditioing = dbHouse.Fetch<AirconditioningEntry>();
            var airConditioningGuids = airconditioing.Select(x => x.HouseGuid).Distinct().ToList();
            MakeAirConditioningSankeySankey();
            MakeAirConditioningMap();

            void MakeAirConditioningSankeySankey()
            {
                var ssa = new SingleSankeyArrow("AirConditioingHouses", 1500, MyStage, SequenceNumber, Name, Services.Logger, slice);
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
                    var ace = airconditioing.FirstOrDefault(x => x.HouseGuid == h.HouseGuid);
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

                var filename = MakeAndRegisterFullFilename("Klimatisierung.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Klimatisierung (GHD)", Constants.Red),
                    new MapLegendEntry("Klimatisierung (Industrie)", Constants.Blue),
                    new MapLegendEntry("Keine Klimatisierung", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<AirconditioningEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            //var dbComplexEnergyData = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            var houses = dbHouses.Fetch<House>();
            var businesses = dbHouses.Fetch<BusinessEntry>();
            //var energy = dbComplexEnergyData.Fetch<MonthlyElectricityUsePerStandort>();
            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                var business = businesses.FirstOrDefault(x => x.HouseGuid == house.HouseGuid);
                if (business != null && business.BusinessType == BusinessType.Shop
                                     && business.LowVoltageYearlyTotalElectricityUse > 20000) {
                    var airconditioningEntry = new AirconditioningEntry(house.HouseGuid, Guid.NewGuid().ToString(),
                        business.LowVoltageYearlyTotalElectricityUse * 0.3, 3, 25, AirConditioningType.Commercial,house.Hausanschluss[0].HausanschlussGuid,house.ComplexName);
                    business.LowVoltageYearlyTotalElectricityUseWithoutAirconditioning = business.LowVoltageYearlyTotalElectricityUse -
                                                                               airconditioningEntry.LowVoltageYearlyTotalElectricityUse;
                    dbHouses.Save(airconditioningEntry);
                    dbHouses.Save(business);
                }

                if (business != null && business.BusinessType == BusinessType.Industrie && business.LowVoltageYearlyTotalElectricityUse > 20000) {
                    var airconditioningEntry = new AirconditioningEntry(house.HouseGuid, Guid.NewGuid().ToString(),
                        business.LowVoltageYearlyTotalElectricityUse * 0.3, 3, 25, AirConditioningType.Industrial,house.Hausanschluss[0].HausanschlussGuid, house.ComplexName);
                    business.LowVoltageYearlyTotalElectricityUseWithoutAirconditioning =
                        business.LowVoltageYearlyTotalElectricityUse - airconditioningEntry.LowVoltageYearlyTotalElectricityUse;
                    dbHouses.Save(airconditioningEntry);
                    dbHouses.Save(business);
                }
            }

            dbHouses.CompleteTransaction();
        }
    }
}