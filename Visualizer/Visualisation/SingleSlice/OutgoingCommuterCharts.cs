using System;
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
    public class OutgoingCommuterCharts : VisualisationBase {
        public OutgoingCommuterCharts([NotNull] IServiceRepository services, Stage stage) : base(nameof(OutgoingCommuterCharts), services, stage)
        {
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouse.Fetch<House>();
            var households = dbHouse.Fetch<Household>();
            var occupants = households.SelectMany(x => x.Occupants).ToList();
            var outgoingCommuters = dbHouse.Fetch<OutgoingCommuterEntry>();
            var carDistances = dbHouse.Fetch<CarDistanceEntry>();
            MakeCarDistanceHistogram();
            MakeCommuterSankey();
            MakeCommuterMap();
            MakeCommuterDistanceHistogram();
            MakeCommuterDistanceHistogramNonBurgdorf();

            void MakeCommuterSankey()
            {
                var ssa = new SingleSankeyArrow("OutgoingCommuters", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Einwohner", occupants.Count, 5000, Orientation.Straight));
                var workersOutside = outgoingCommuters.Count(x => x.DistanceInKm > 0);
                var workersBurgdorf = outgoingCommuters.Count(x => Math.Abs(x.DistanceInKm) < 0.000001);
                var unemployed = occupants.Count - workersOutside - workersBurgdorf;
                ssa.AddEntry(new SankeyEntry("Ohne Anstellung", unemployed * -1, 2000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Pendler aus Burgdorf", workersOutside * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Arbeiter in Burgdorf", workersBurgdorf * -1, 2000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeCarDistanceHistogram()
            {
                var commuterDistances = carDistances.Select(x => x.TotalDistance).ToList();
                var hg = new Histogram(commuterDistances, 15);
                var bse = new List<BarSeriesEntry>();
                var barSeries = BarSeriesEntry.MakeBarSeriesEntry(hg, out var colNames, "F1");
                bse.Add(barSeries);
                var filename = MakeAndRegisterFullFilename("CarDistanceHistogram.png", slice);
                Services.PlotMaker.MakeBarChart(filename, "Anzahl Autos mit dieser Entfernung", bse, colNames);
            }

            void MakeCommuterDistanceHistogram()
            {
                var commuterDistances = outgoingCommuters.Select(x => x.DistanceInKm).ToList();
                var hg = new Histogram(commuterDistances, 15);
                var bse = new List<BarSeriesEntry>();
                var barSeries = BarSeriesEntry.MakeBarSeriesEntry(hg, out var colNames, "F1");
                bse.Add(barSeries);
                var filename = MakeAndRegisterFullFilename("OutgoingCommutersDistanceHistogram.png", slice);
                Services.PlotMaker.MakeBarChart(filename, "Anzahl Pendler mit dieser Entfernung", bse, colNames);
            }


            void MakeCommuterDistanceHistogramNonBurgdorf()
            {
                var commuterDistances = outgoingCommuters.Where(x => x.DistanceInKm > 0).Select(x => x.DistanceInKm).ToList();
                var hg = new Histogram(commuterDistances, 15);
                var bse = new List<BarSeriesEntry>();
                var barSeries = BarSeriesEntry.MakeBarSeriesEntry(hg, out var colNames, "F1");
                bse.Add(barSeries);
                var filename = MakeAndRegisterFullFilename("OutgoingCommutersDistanceHistogramNoBurgdorf.png", slice);
                Services.PlotMaker.MakeBarChart(filename, "Anzahl Pendler mit dieser Entfernung", bse, colNames);
            }

            void MakeCommuterMap()
            {
                RGBWithSize GetColor(House h)
                {
                    var outgoing = outgoingCommuters.Count(x => x.HouseGuid == h.Guid);
                    if (outgoing > 0) {
                        return new RGBWithSize(Constants.Red, outgoing + 10);
                    }

                    return new RGBWithSize(Constants.Black, 10);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("OutgoingCommutersMap.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Arbeiter", Constants.Red),
                    new MapLegendEntry("Keine Pendler", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}