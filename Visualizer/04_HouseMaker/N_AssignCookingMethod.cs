using System;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer.Sankey;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class N_AssignCookingMethod : RunableWithBenchmark {
        public N_AssignCookingMethod([NotNull] ServiceRepository services)
            : base(nameof(N_AssignCookingMethod), Stage.Houses, 1400, services, false)
        {
            DevelopmentStatus.Add("//odo: turn into house infrastructure entry if its not cooking");
            DevelopmentStatus.Add("//odo: do the cooking implementation properly");
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var potentialHeatingSystems = dbHouse.Fetch<PotentialHeatingSystemEntry>();
            MakeCookingSankey();

            //HeatingSystemCountHistogram();
            //MakeHeatingSystemMapError();
            //MakeHeatingSystemMap();
            void MakeCookingSankey()
            {
                var ssa = new SingleSankeyArrow("PotentialCookingSystems", 1500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var count = 0;
                foreach (var entry in potentialHeatingSystems) {
                    if (entry.YearlyGasDemand < 1000) {
                        count++;
                    }
                }

                ssa.AddEntry(new SankeyEntry("Kochgas", count * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Elektrisches Kochen", (houses.Count - count) * -1, 2000, Orientation.Straight));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            /*
            void MakeHeatingSystemMapError()
            {
                RGB GetColor(House h)
                {
                    HeatingSystemEntry hse = heatingsystems.Single(x => x.HouseGuid == h.HouseGuid);
                    if (hse.HeatingSystemType == HeatingSystemType.Fernwärme)
                    {
                        return Constants.Red;
                    }
                    if (hse.HeatingSystemType == HeatingSystemType.Gasheating)
                    {
                        return Constants.Orange;
                    }
                    if (hse.HeatingSystemType == HeatingSystemType.FeuerungsstättenGas)
                    {
                        return Constants.Orange;
                    }
                    return Constants.Black;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("KantonHeatingSystemErrors.svg", Name, "");
                List<MapLegendEntry> legendEntries = new List<MapLegendEntry>();
                legendEntries.Add(new MapLegendEntry("Kanton Fernwärme", Constants.Red));
                legendEntries.Add(new MapLegendEntry("Kanton Gas", Constants.Orange));
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
            void MakeHeatingSystemMap()
            {
                Dictionary<HeatingSystemType, RGB> rgbs = new Dictionary<HeatingSystemType, RGB>();
                var hs = heatingsystems.Select(x => x.HeatingSystemType).Distinct().ToList();
                ColorGenerator cg = new ColorGenerator();
                int idx = 0;
                foreach (HeatingSystemType type in hs)
                {
                    rgbs.Add(type, cg.GetRGB(idx++));
                }
                RGB GetColor(House h)
                {
                    HeatingSystemEntry hse = heatingsystems.Single(x => x.HouseGuid == h.HouseGuid);
                    return rgbs[hse.HeatingSystemType];
                }
                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("HeatingSystemMap.svg", Name, "");
                List<MapLegendEntry> legendEntries = new List<MapLegendEntry>();
                foreach (var pair in rgbs)
                {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }*/
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<CookingMethodEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var households = dbHouses.Fetch<Household>();
            var potentialCookingSystemEntry = dbHouses.Fetch<PotentialCookingSystemEntry>();
            dbHouses.BeginTransaction();
            foreach (var entry in potentialCookingSystemEntry) {
                var hhs = households.Where(x => x.Standorte.Contains(entry.Standort)).ToList();
                if (hhs.Count > 1) {
                    throw new Exception("More than 1 household found");
                }

                if (hhs.Count == 1) {
                    var cme = new CookingMethodEntry();
                    var hh = hhs[0];
                    cme.HouseGuid = hh.HouseGuid;
                    cme.HouseholdGuid = hh.HouseholdGuid;
                    cme.CookingMethod = CookingMethod.CookByGas;
                    dbHouses.Save(cme);
                }
                // ReSharper disable once RedundantIfElseBlock
                else {
                    //odo: turn into house infrastructure entry
                }
            }

            dbHouses.CompleteTransaction();
        }
    }
}