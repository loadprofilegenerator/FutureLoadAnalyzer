using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using MathNet.Numerics.Statistics;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace Visualizer.Visualisation.SingleSlice {
    public class OccupantCharts : VisualisationBase {
        public OccupantCharts([NotNull] IServiceRepository services, Stage stage) : base(nameof(OccupantCharts), services, stage)
        {
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            //var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, parameters);
            var houses = dbHouse.Fetch<House>();
            var households = dbHouse.Fetch<Household>();
            MakeAgeHistogram();
            MakePeopleCountMap2();
            MakeFamilySizeSankey();
            MakePeopleCountMap();
            MakeAgeMap();

            void MakeAgeHistogram()
            {
                List<Occupant> occupants = households.SelectMany(x => x.Occupants).ToList();
                double maxAge = occupants.Max(x => x.Age);
                var histogram = new Histogram();
                const int bucketSize = 5;
                for (var i = 0; i < maxAge; i += bucketSize) {
                    histogram.AddBucket(new Bucket(i, i + bucketSize));
                }

                var ages = occupants.Select(x => (double)x.Age).ToList();
                histogram.AddData(ages);
                var filename = MakeAndRegisterFullFilename("Altersverteilung.png", slice);
                var bs = BarSeriesEntry.MakeBarSeriesEntry(histogram, out var colNames);
                var bss = new List<BarSeriesEntry> {
                    bs
                };
                Services.PlotMaker.MakeBarChart(filename, "Anzahl von Personen in diesem Altersbereich", bss, colNames);
            }

            void MakeFamilySizeSankey()
            {
                var ssa = new SingleSankeyArrow("Households", 1000, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Households", houses.Count, 5000, Orientation.Straight));
                var counts = households.Select(x => x.Occupants.Count).ToList();
                var maxSize = counts.Max();
                var filename = MakeAndRegisterFullFilename("HouseholdSizeHistogram.png", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                for (var i = 0; i < maxSize + 1; i++) {
                    names.Add(i.ToString());
                    var j = i; // because of closure
                    var count = counts.Count(x => x == j);
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(i + "Personen", count, i));
                }

                Services.PlotMaker.MakeBarChart(filename, "", barSeries, names);
            }

            void MakePeopleCountMap()
            {
                RGB GetColor(House h)
                {
                    var hhs = households.Where(x => x.HouseGuid == h.Guid).ToList();
                    var peopleCount = hhs.Select(x => x.Occupants.Count).Sum();
                    if (peopleCount == 0) {
                        return new RGB(128, 128, 128);
                    }

                    if (peopleCount == 1) {
                        return Constants.Green;
                    }

                    if (peopleCount == 2) {
                        return Constants.Blue;
                    }

                    return Constants.Red;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("AnzahlPersonenProHaushalt.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Keine Einwohner", new RGB(128, 128, 128)),
                    new MapLegendEntry("Ein Einwohner", Constants.Green),
                    new MapLegendEntry("Zwei Einwohner", Constants.Blue),
                    new MapLegendEntry("Drei+ Einwohner", Constants.Red)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }

            void MakePeopleCountMap2()
            {
                var maxCount = 0;
                foreach (var house in houses) {
                    var hhs = households.Where(x => x.HouseGuid == house.Guid).ToList();
                    var sumCount = hhs.Sum(x => x.Occupants.Count);
                    if (maxCount < sumCount) {
                        maxCount = sumCount;
                    }
                }

                var colorStep = 250.0 / maxCount;
                double color = 0;
                var colorDict = new Dictionary<int, int>();
                for (var i = 1; i <= maxCount; i++) {
                    colorDict.Add(i, (int)color);
                    color += colorStep;
                }

                RGBWithSize GetColor(House h)
                {
                    var hhs = households.Where(x => x.HouseGuid == h.Guid).ToList();
                    var peopleCount = hhs.Select(x => x.Occupants.Count).Sum();
                    if (peopleCount == 0) {
                        return new RGBWithSize(128, 128, 128, 10);
                    }

                    var red = colorDict[peopleCount];
                    var size = 10;
                    if (peopleCount > size) {
                        size = peopleCount;
                    }

                    return new RGBWithSize(red, 0, 0, size);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("AnzahlPersonenProHaushaltRelative.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Keine Einwohner", new RGB(128, 128, 128))
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }

            void MakeAgeMap()
            {
                RGB GetColor(House h)
                {
                    var hhs = households.Where(x => x.HouseGuid == h.Guid).ToList();
                    var houseoccupants = hhs.SelectMany(x => x.Occupants).ToList();
                    double averageAge = 0;
                    if (houseoccupants.Count > 0) {
                        averageAge = houseoccupants.Average(x => x.Age);
                    }

                    if (houseoccupants.Count == 0) {
                        return new RGB(128, 128, 128);
                    }

                    var agefactor = averageAge / 100;
                    if (agefactor > 1) {
                        agefactor = 1;
                    }

                    return new RGB((int)(255.0 * agefactor), 0, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("Durchschnittsalter.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Keine Einwohner", new RGB(128, 128, 128)),
                    new MapLegendEntry("0 Jahre Durchschnittsalter", new RGB(0, 0, 0)),
                    new MapLegendEntry("100 Jahre Durchschnittsalter", new RGB(255, 0, 0))
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}