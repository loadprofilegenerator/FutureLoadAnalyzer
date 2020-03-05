using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Map;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class A02_MatchOsmToHouse : RunableWithBenchmark {
        [NotNull] [ItemNotNull] private readonly List<WgsPoint> _notDirectHits = new List<WgsPoint>();

        public A02_MatchOsmToHouse([NotNull] ServiceRepository services)
            : base(nameof(A02_MatchOsmToHouse), Stage.Houses, 2, services, true)
        {
        }

        protected override void RunChartMaking()
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var finishedMatches = dbHouse.Fetch<HouseOsmMatch>();
            var ces = new List<MapColorEntryWithOsmGuid>();
            var count = 1;
            foreach (var matches in finishedMatches) {
                if (matches.MatchType == MatchType.GWRMatch) {
                    ces.Add(new MapColorEntryWithOsmGuid(matches.OsmGuid, Constants.Green));
                }
                else {
                    ces.Add(new MapColorEntryWithOsmGuid(matches.OsmGuid, Constants.Blue, count.ToString()));
                    count++;
                }
            }

            var filename = MakeAndRegisterFullFilename("OSMMap.png", Constants.PresentSlice);
            var labels = new List<MapLegendEntry> {
                new MapLegendEntry("Direkt Gemappt", Constants.Green),
                new MapLegendEntry("Näherung", Constants.Blue),
                new MapLegendEntry("Kein GWR-Gebäude", Constants.Red),
                new MapLegendEntry("Originaler GWR-Punkt", Constants.Türkis)
            };
            Services.PlotMaker.MakeOsmMap(Name, filename, ces, _notDirectHits, labels,new List<LineEntry>());
        }


        protected override void RunActualProcess()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouse.RecreateTable<HouseOsmMatch>();
            var osmFeatures = dbRaw.Fetch<OsmFeature>();
            var houses = dbHouse.Fetch<House>();

            var repository = new MapTileRepository(osmFeatures);
            dbHouse.BeginTransaction();
            foreach (var house in houses) {
                //find gwr entries matching the house
                //var gwrEntries = gwr.Where(x => house.EGIDs.Contains(x.EidgGebaeudeidentifikator_EGID ?? 0));
                var filteredPoints = house.WgsGwrCoords.Where(x => repository.BoundingBoxAllFeatures.IsInside(x)).ToList();
                if (filteredPoints.Count == 0) {
                    continue;
                }

                var matchingOsms = repository.FindDirectlyMatchingFeatures(filteredPoints);
                var matchType = MatchType.GWRMatch;
                double distance = 0;
                if (matchingOsms.Count == 0) {
                    var closestFeature = repository.FindBestDistanceMatch(filteredPoints, out var closestPoint, out distance);
                    matchType = MatchType.GWRClosest;
                    matchingOsms.Add(closestFeature);
                    var point = closestPoint;
                    point.Size = 10;
                    point.Label = "blub";
                    point.Rgb = Constants.Türkis;
                    _notDirectHits.Add(point);
                }

                foreach (var feature in matchingOsms) {
                    var hom = new HouseOsmMatch(house.Guid, feature.Guid, matchType, distance);
                    dbHouse.Save(hom);
                }
            }

            dbHouse.CompleteTransaction();
        }
    }
}