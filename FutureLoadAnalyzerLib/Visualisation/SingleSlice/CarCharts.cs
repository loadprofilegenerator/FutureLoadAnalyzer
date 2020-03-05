using System.Collections.Generic;
using System.Linq;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Visualisation.SingleSlice {
    public class CarCharts : VisualisationBase {
        public CarCharts([NotNull] ServiceRepository services, Stage myStage) : base(nameof(CarCharts), services, myStage)
        {
            DevelopmentStatus.Add("Maps are messed up");
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouse.Fetch<House>();
            var households = dbHouse.Fetch<Household>();
            var cars = dbHouse.Fetch<Car>();
            if (cars.Count == 0) {
                Error("No cars were found for scenario " + slice);
                return;
            }

            MakeCarAmountSankey();
            CarCountHistogram();
            MakeCarMap();

            void MakeCarAmountSankey()
            {
                var ssa = new SingleSankeyArrow("HouseholdsWithCar", 1000, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Households", households.Count, 5000, Orientation.Straight));
                var autos = cars.Count;
                ssa.AddEntry(new SankeyEntry("Mit Auto", autos * -1, 5000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Ohne Auto", (households.Count - autos) * -1, 5000, Orientation.Straight));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void CarCountHistogram()
            {
                var carCountsPerHouse = new List<int>();
                foreach (var house in houses) {
                    var carsInHouse = cars.Where(x => x.HouseGuid == house.Guid).ToList();
                    carCountsPerHouse.Add(carsInHouse.Count);
                }

                var maxSize = carCountsPerHouse.Max();
                var filename = MakeAndRegisterFullFilename("CarsPerHouseHistogram.png", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                for (var i = 0; i < maxSize + 1; i++) {
                    names.Add(i.ToString());
                    var j = i;
                    var count = carCountsPerHouse.Count(x => x == j);
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(i + " Autos", count, i));
                }

                Services.PlotMaker.MakeBarChart(filename, "CarsPerHouseHistogram", barSeries, names);
            }

            void MakeCarMap()
            {
                var mapPoints = new List<MapPoint>();
                foreach (var house in houses) {
                    if (house.WgsGwrCoords.Count == 0) {
                        continue;
                    }

                    var carsInHouse = cars.Count(x => x.HouseGuid == house.Guid);
                    mapPoints.Add(new MapPoint(house.WgsGwrCoords[0].Lon, house.WgsGwrCoords[0].Lat, carsInHouse, carsInHouse + 10));
                }

                var filename = MakeAndRegisterFullFilename("AutosProHaus.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}