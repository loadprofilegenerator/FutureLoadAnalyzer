using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class A08_MatchOsmToHouse : RunableWithBenchmark {
        public A08_MatchOsmToHouse([NotNull] ServiceRepository services)
            : base(nameof(A08_MatchOsmToHouse), Stage.Houses, 8, services, false)
        {
            DevelopmentStatus.Add("Do something about the big red buildings in the industry area");
        }

        [NotNull] [ItemNotNull] private readonly List<WgsPoint> _notDirectHits = new List<WgsPoint>();

        protected override void RunChartMaking()
        {
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var finishedMatches = dbHouse.Fetch<HouseOsmMatch>();
            var ces = new List<MapColorEntryWithOsmGuid>();
            var count = 1;
            foreach (var matches in finishedMatches) {
                if (matches.MatchType == MatchType.LocalnetMatch) {
                    ces.Add(new MapColorEntryWithOsmGuid(matches.OsmGuid, Constants.Green));
                }
                else if (matches.MatchType == MatchType.LocalnetClosest) {
                    ces.Add(new MapColorEntryWithOsmGuid(matches.OsmGuid, Constants.Blue, count.ToString()));
                    count++;
                }
            }

            var filename = MakeAndRegisterFullFilename("OSMMap.png", Name, "", Constants.PresentSlice);
            var labels = new List<MapLegendEntry> {
                new MapLegendEntry("Direkt Gemappt", Constants.Green),
                new MapLegendEntry("Näherung", Constants.Blue),
                new MapLegendEntry("Kein Localnet-Gebäude", Constants.Red),
                new MapLegendEntry("Originaler Localnet-Punkt", Constants.Türkis)
            };

            Services.PlotMaker.MakeOsmMap(Name, filename, ces, _notDirectHits, labels, new List<LineEntry>());
        }


        protected override void RunActualProcess()
        {
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            dbHouse.Execute("DELETE FROM HouseOsmMatch where MatchType = " + (int)MatchType.LocalnetClosest);
            dbHouse.Execute("DELETE FROM HouseOsmMatch where MatchType = " + (int)MatchType.LocalnetMatch);


            var osmFeatures = dbRaw.Fetch<OsmFeature>();
            var houses = dbHouse.Fetch<House>();

            var repository = new MapTileRepository(osmFeatures);
            dbHouse.BeginTransaction();
            foreach (var house in houses) {
                //find gwr entries matching the house
                //var gwrEntries = gwr.Where(x => house.EGIDs.Contains(x.EidgGebaeudeidentifikator_EGID ?? 0));
                var filteredPoints = house.LocalWgsPoints.Where(x => repository.BoundingBoxAllFeatures.IsInside(x)).ToList();
                if (filteredPoints.Count == 0) {
                    continue;
                }

                var matchingOsms = repository.FindDirectlyMatchingFeatures(filteredPoints);
                var matchType = MatchType.LocalnetMatch;
                double distance = 0;
                if (matchingOsms.Count == 0) {
                    var closestFeature = repository.FindBestDistanceMatch(filteredPoints, out var closestPoint, out distance);
                    matchType = MatchType.LocalnetClosest;
                    matchingOsms.Add(closestFeature);
                    var point = closestPoint;
                    point.Size = 10;
                    point.Label = "blub";
                    point.Rgb = Constants.Türkis;
                    _notDirectHits.Add(point);
                }

                foreach (var feature in matchingOsms) {
                    var hom = new HouseOsmMatch(house.HouseGuid, feature.Guid, matchType, distance);
                    dbHouse.Save(hom);
                }
            }

            dbHouse.CompleteTransaction();
        }
    }
}