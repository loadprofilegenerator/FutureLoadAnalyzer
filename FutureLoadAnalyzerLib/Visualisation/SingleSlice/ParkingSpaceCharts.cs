using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Visualisation.SingleSlice {
    public class ParkingSpaceCharts : VisualisationBase {
        public ParkingSpaceCharts([NotNull] ServiceRepository services, Stage myStage) : base(nameof(ParkingSpaceCharts), services,myStage)
        {
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouse.Fetch<House>();
            var households = dbHouse.Fetch<Household>();
            var parkingSpaces = dbHouse.Fetch<ParkingSpace>();
            MakeCarAmountSankey();
            CarCountHistogram();
            MakeCarMap();

            void MakeCarAmountSankey()
            {
                var ssa = new SingleSankeyArrow("HouseholdsWithParkingSpace", 1000, MyStage, SequenceNumber,
                    Name,  slice, Services);
                ssa.AddEntry(new SankeyEntry("Households", households.Count, 5000, Orientation.Straight));
                var autos = parkingSpaces.Count;
                ssa.AddEntry(new SankeyEntry("Mit Parkieren", autos * -1, 5000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Ohne Parkieren", (households.Count - autos) * -1, 5000, Orientation.Straight));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void CarCountHistogram()
            {
                var carCountsPerHouse = new List<int>();
                foreach (var house in houses)
                {
                    var carsInHouse = parkingSpaces.Where(x => x.HouseGuid == house.Guid).ToList();
                    carCountsPerHouse.Add(carsInHouse.Count);
                }

                var maxSize = carCountsPerHouse.Max();
                var filename = MakeAndRegisterFullFilename("ParkingPerHouseHistogram.png", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                for (var i = 0; i < maxSize + 1; i++)
                {
                    names.Add(i.ToString());
                    var j = i;
                    var count = carCountsPerHouse.Count(x => x == j);
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(i + " Autos", count, i));
                }

                Services.PlotMaker.MakeBarChart(filename, "ParkingPerHouseHistogram", barSeries, names);
            }

            void MakeCarMap()
            {
                var mapPoints = new List<MapPoint>();

                RGBWithSize GetMapPoint(House h)
                {
                    var carsInHouse = parkingSpaces.Count(x => x.HouseGuid == h.Guid);
                    return new RGBWithSize(Constants.Red, carsInHouse + 10);

                }

                foreach (var house in houses)
                {
                    mapPoints.Add(house.GetMapPointWithSize(GetMapPoint));
                }

                var filename = MakeAndRegisterFullFilename("AutosProHaus.svg", slice);
                var legendEntries = new List<MapLegendEntry>();
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}