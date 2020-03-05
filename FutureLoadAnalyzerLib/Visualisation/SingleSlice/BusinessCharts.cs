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

namespace FutureLoadAnalyzerLib.Visualisation.SingleSlice
{
    public class BusinessCharts : VisualisationBase {
        public BusinessCharts([NotNull] ServiceRepository services, Stage myStage) : base(nameof(BusinessCharts),services,myStage)
        {
            DevelopmentStatus.Add("Maps are messed up");
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouse.Fetch<House>();
            var business = dbHouse.Fetch<BusinessEntry>();
            if (business.Count == 0) {
                throw new FlaException("No businesses");
            }
            MakeBusinessSankey();
            MakeHouseholdsMap();
            MakeBusinessTypeHistogram();

            void MakeBusinessTypeHistogram()
            {
                var counts = new Dictionary<BusinessType, int>();
                foreach (var entry in business)
                {
                    if (!counts.ContainsKey(entry.BusinessType))
                    {
                        counts.Add(entry.BusinessType, 0);
                    }

                    counts[entry.BusinessType]++;
                }

                var filename = MakeAndRegisterFullFilename("BusinessHistogram.png", slice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                var column = 0;
                foreach (var pair in counts)
                {
                    names.Add(pair.Key.ToString());
                    var count = pair.Value;
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(pair.Key.ToString(), count, column));
                    column++;
                }

                Services.PlotMaker.MakeBarChart(filename, "BusinessHistogram", barSeries, names);
            }

            void MakeBusinessSankey()
            {
                var ssa = new SingleSankeyArrow("Houses", 1000, MyStage,
                    SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses total", houses.Count, 5000, Orientation.Straight));
                var housesWithZeroBusinesses = 0;
                var housesWithOneBusiness = 0;
                var housesWithManyBusiness = 0;
                foreach (var house in houses)
                {
                    var businessesForHouse = business.Where(x => x.HouseGuid == house.Guid).ToList();
                    if (businessesForHouse.Count == 0)
                    {
                        housesWithZeroBusinesses++;
                    }
                    else if (businessesForHouse.Count == 1)
                    {
                        housesWithOneBusiness++;
                    }
                    else
                    {
                        housesWithManyBusiness++;
                    }
                }

                ssa.AddEntry(new SankeyEntry("Häuser mit 0 Geschäften", housesWithZeroBusinesses * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Häuser mit 1 Geschäft", housesWithOneBusiness * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Mehrere Geschäfte", housesWithManyBusiness * -1, 5000, Orientation.Up));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHouseholdsMap()
            {
                RGB GetColor(House h)
                {
                    var s = business.Where(x => x.HouseGuid == h.Guid).ToList();
                    if (s.Count == 0)
                    {
                        return new RGB(255, 0, 0);
                    }

                    var multiplier = 1;
                    if (s.Any(x => x.BusinessType == BusinessType.Industrie))
                    {
                        multiplier = 2;
                    }

                    if (s.Count == 1)
                    {
                        return new RGB(0, 0, 125 * multiplier);
                    }

                    return new RGB(0, 125 * multiplier, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHouseholdCountsPerHouse.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("No Business", 255, 0, 0),
                    new MapLegendEntry("Genau 1 Business", 0, 0, 255),
                    new MapLegendEntry("Viele Business", 0, 255, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }

}
