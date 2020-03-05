using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Visualisation.SingleSlice
{
    public class HouseholdCharts : VisualisationBase {
        public HouseholdCharts([NotNull] IServiceRepository serivces, Stage myStage ) : base(nameof(HouseholdCharts), serivces,myStage)
        {
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice,
                                                  bool isPresent)
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var gwrData = dbRaw.Fetch<GwrData>();
            var houses = dbHouse.Fetch<House>();
            if (houses.Count == 0) {
                throw new FlaException("No houses found");
            }
            var households = dbHouse.Fetch<Household>();
            MakeHouseholdsDistributionChart();
            MakeHouseholdsMap();
            HouseholdAppartmentSankey();
            MakeHouseholdsGWRMapComparison();

            void MakeHouseholdsDistributionChart()
            {
                var ssa = new SingleSankeyArrow("Houses", 1000, MyStage, SequenceNumber, Name,
                    slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses total", houses.Count, 5000, Orientation.Straight));
                var housesWithZeroHouseholds = 0;
                var housesWithOneHousehold = 0;
                var housesWithManyHouseholds = 0;
                foreach (var house in houses)
                {
                    var houeholdsForHouse = households.Where(x => x.HouseGuid == house.Guid).ToList();
                    if (houeholdsForHouse.Count == 0)
                    {
                        housesWithZeroHouseholds++;
                    }
                    else if (houeholdsForHouse.Count == 1)
                    {
                        housesWithOneHousehold++;
                    }
                    else
                    {
                        housesWithManyHouseholds++;
                    }
                }

                ssa.AddEntry(new SankeyEntry("Häuser mit 0 Haushalten", housesWithZeroHouseholds * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Häuser mit 1 Haushalt", housesWithOneHousehold * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("MFH", housesWithManyHouseholds * -1, 5000, Orientation.Up));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHouseholdsMap()
            {
                RGB GetColor(House h)
                {
                    var s = households.Where(x => x.HouseGuid == h.Guid).ToList();
                    if (s.Count == 0)
                    {
                        return new RGB(255, 0, 0);
                    }

                    if (s.Count == 1)
                    {
                        return new RGB(0, 0, 255);
                    }

                    return new RGB(0, 255, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHouseholdCountsPerHouse.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("No Household", 255, 0, 0),
                    new MapLegendEntry("Genau 1 Haushalt", 0, 0, 255),
                    new MapLegendEntry("Viele Haushalte", 0, 255, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }

            void HouseholdAppartmentSankey()
            {
                var householdCount = households.Count;
                var gwrAppartmentCount = gwrData.Sum(x => x.AnzahlWohnungen_GANZWHG ?? 0);
                var diff = gwrAppartmentCount - householdCount;
                var ssa = new SingleSankeyArrow("GWRApartmentsVsHouseholds", 5000, MyStage,
                    SequenceNumber,Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("GWR", gwrAppartmentCount, 1500, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Haushalte", householdCount * -1, 1500, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Rest", diff * -1, 500, Orientation.Up));

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHouseholdsGWRMapComparison()
            {
                RGB GetColor(House h)
                {
                    var householdCount = households.Count(x => x.HouseGuid == h.Guid);
                    var gwrAppartmentList = gwrData.Where(x => h.EGIDs.Contains(x.EidgGebaeudeidentifikator_EGID ?? 0)).ToList();
                    var gwrAppartmentCount = gwrAppartmentList.Sum(x => x.AnzahlWohnungen_GANZWHG);
                    var diff = householdCount - gwrAppartmentCount;
                    if (diff == 0)
                    {
                        return new RGB(64, 64, 64);
                    }

                    if (diff > 1)
                    {
                        return new RGB(0, 0, 255);
                    }

                    return new RGB(255, 0, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapHouseholdCountVsGwrHouseholds.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Same count", 64, 64, 64),
                    new MapLegendEntry("Mehr GWR", 0, 0, 255),
                    new MapLegendEntry("Mehr Haushalte", 255, 0, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}
