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
    public class K_InitialStrassenbeleuchtung : RunableWithBenchmark {
        public K_InitialStrassenbeleuchtung([NotNull] ServiceRepository services)
            : base(nameof(K_InitialStrassenbeleuchtung), Stage.Houses, 1100, services, false)
        {
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var lights = dbHouse.Fetch<StreetLightingEntry>();
            var lightHouseGuids = lights.Select(x => x.HouseGuid).Distinct().ToList();
            MakeLightSankey();
            MakeCommuterMap();

            void MakeLightSankey()
            {
                var ssa = new SingleSankeyArrow("LightingHouses", 1500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var housesWithLight = lightHouseGuids.Count;

                ssa.AddEntry(new SankeyEntry("Strassenbeleuchtungen", housesWithLight * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Sonstige Häuser", (houses.Count - housesWithLight) * -1, 2000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakeCommuterMap()
            {
                RGBWithSize GetColor(House h)
                {
                    if (lightHouseGuids.Contains(h.HouseGuid)) {
                        return new RGBWithSize(Constants.Red, 20);
                    }

                    return new RGBWithSize(Constants.Black, 10);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor, House.CoordsToUse.Localnet)).ToList();

                var filename = MakeAndRegisterFullFilename("Strassenbeleuchtung.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Strassenbeleuchtung", Constants.Red),
                    new MapLegendEntry("Sonstiges", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }

        protected override void RunActualProcess()
        {
            //stassenbeleuchtungen werden von den localnet daten bereits gesetzt, hier nur visualisierung
        }
    }
}