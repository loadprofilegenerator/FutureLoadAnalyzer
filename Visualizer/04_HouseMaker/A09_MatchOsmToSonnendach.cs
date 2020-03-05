using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class A09_MatchOsmToSonnendach : RunableWithBenchmark {
        public A09_MatchOsmToSonnendach([NotNull] ServiceRepository services)
            : base(nameof(A09_MatchOsmToSonnendach), Stage.Houses, 9, services, false)
        {
            DevelopmentStatus.Add("Match closest house by distacne");
            DevelopmentStatus.Add("Match localnet entries directly");
            DevelopmentStatus.Add("Match localnet entries by distance");
        }

        [NotNull] [ItemNotNull] private readonly List<WgsPoint> _notDirectHits = new List<WgsPoint>();

        protected override void RunChartMaking()
        {
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //make chart
            //has to be here because it needs the list of not matching points
            var finishedMatches = dbHouse.Fetch<HouseOsmMatch>();
            var ces = new List<MapColorEntryWithOsmGuid>();
            foreach (var matches in finishedMatches) {
                if (matches.MatchType == MatchType.SonnendachDirect) {
                    ces.Add(new MapColorEntryWithOsmGuid(matches.OsmGuid, Constants.Green));
                }
                else if (matches.MatchType == MatchType.SonnedachClosest) {
                    ces.Add(new MapColorEntryWithOsmGuid(matches.OsmGuid, Constants.Blue));
                }
            }

            var filename = MakeAndRegisterFullFilename("OSMMap.png", Name, "", Constants.PresentSlice);
            var labels = new List<MapLegendEntry> {
                new MapLegendEntry("Direkt Gemappt", Constants.Green),
                new MapLegendEntry("Näherung", Constants.Blue),
                new MapLegendEntry("Kein Sonnendach-Gebäude", Constants.Red),
                new MapLegendEntry("Originaler Sonnendach-Punkt", Constants.Türkis)
            };

            Services.PlotMaker.MakeOsmMap(Name, filename, ces, _notDirectHits, labels, new List<LineEntry>());
        }

        protected override void RunActualProcess()
        {
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            dbHouse.Execute("DELETE FROM HouseOsmMatch where MatchType ='" + (int)MatchType.SonnendachDirect + "'");
            dbHouse.Execute("DELETE FROM HouseOsmMatch where MatchType ='" + (int)MatchType.SonnedachClosest + "'");
            var osmFeatures = dbRaw.Fetch<OsmFeature>();
            var sonnendach = dbRaw.Fetch<B05_SonnendachGeoJson>();
            dbHouse.BeginTransaction();
            var repository = new MapTileRepository(osmFeatures);

            foreach (var sonnendachEntry in sonnendach) {
                if (sonnendachEntry.WgsPoints.Count == 0) {
                    throw new Exception("No wgs points in Sonnendach");
                }

                var pointsToLookFor = sonnendachEntry.WgsPoints.Where(x => repository.BoundingBoxAllFeatures.IsInside(x)).ToList();
                if (pointsToLookFor.Count == 0) {
                    //points outside of osm
                    continue;
                }

                var matchingOsms = repository.FindDirectlyMatchingFeatures(pointsToLookFor);
                var mt = MatchType.SonnendachDirect;
                double distance = 0;
                if (matchingOsms.Count == 0) {
                    var closestFeature = repository.FindBestDistanceMatch(pointsToLookFor, out var closestPoint, out distance);
                    matchingOsms.Add(closestFeature);
                    mt = MatchType.SonnedachClosest;
                    var point = closestPoint;
                    point.Size = 10;
                    point.Label = "blub";
                    point.Rgb = Constants.Türkis;
                    //notDirectHits.Add(point);
                }

                foreach (var feature in matchingOsms) {
                    var hom = new HouseOsmMatch(sonnendachEntry.Guid, feature.Guid, mt, distance);
                    dbHouse.Save(hom);
                }
            }

            dbHouse.CompleteTransaction();
        }
    }
}