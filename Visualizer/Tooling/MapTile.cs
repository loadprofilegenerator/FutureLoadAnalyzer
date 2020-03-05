using System;
using System.Collections.Generic;
using Data.DataModel;
using GeoJSON.Net.Geometry;
using JetBrains.Annotations;
using Visualizer.OSM;
using Xunit;

namespace BurgdorfStatistics.Tooling {
    public class MapTile {
        public MapTile()
        {
        }

        public MapTile(double leftLatLat, double topLonLon, double bottomLonLon, double rightLatLat)
        {
            LeftLat = leftLatLat;
            TopLon = topLonLon;
            BottomLon = bottomLonLon;
            RightLat = rightLatLat;
        }

        public double BottomLon { get; set; }

        public double LeftLat { get; set; }

        [NotNull]
        [ItemNotNull]
        public List<OsmFeature> OsmFeaturesInRectangle { get; } = new List<OsmFeature>();
        public double RightLat { get; set; }
        public double TopLon { get; set; }

        public bool FindDirectlyMatchingPolygon([NotNull] [ItemNotNull] List<WgsPoint> points, [CanBeNull] out OsmFeature firstmatchingFeature)
        {
            foreach (var point in points) {
                foreach (var feature in OsmFeaturesInRectangle) {
                    if (IsPointInPolygon(feature.WgsPoints, point)) {
                        firstmatchingFeature = feature;
                        return true;
                    }
                }
            }

            firstmatchingFeature = null;
            return false;
        }

        [NotNull]
        [ItemNotNull]
        public List<OsmFeatureToPointDistance> GetDistances([NotNull] [ItemNotNull] IReadOnlyCollection<WgsPoint> pointsToLookFor)
        {
            if (OsmFeaturesInRectangle.Count == 0) {
                return new List<OsmFeatureToPointDistance>();
            }

            if (pointsToLookFor.Count == 0) {
                throw new Exception("no points to look for");
            }

            var distancePoints = new List<OsmFeatureToPointDistance>();
            foreach (var feature in OsmFeaturesInRectangle) {
                if (feature.Feature.Geometry is Polygon) {
                    OsmFeatureToPointDistance mdc = null;
                    var distance = double.MaxValue;
                    foreach (var point in pointsToLookFor) {
                        var tempmdc = new OsmFeatureToPointDistance(point, feature);
                        if (distance > tempmdc.Distance) {
                            distance = tempmdc.Distance;
                            mdc = tempmdc;
                        }
                    }

                    if (mdc != null) {
                        distancePoints.Add(mdc);
                    }
                }
            }

            if (distancePoints.Count == 0) {
                throw new Exception("no distance found");
            }

            return distancePoints;
        }

        public bool IsInside([NotNull] WgsPoint point)
        {
            if (point.Lon <= TopLon && point.Lon >= BottomLon && point.Lat >= LeftLat && point.Lat <= RightLat) {
                return true;
            }

            return false;
        }

        [Fact]
        public void TestIsInside()
        {
            TopLon = 8;
            BottomLon = 7;
            LeftLat = 47;
            RightLat = 48;
            Assert.True(IsInside(new WgsPoint(7.5, 47.5)));
        }

        public override string ToString() => "T:" + TopLon + " L" + LeftLat + " B" + BottomLon + " R" + RightLat;

        private bool IsPointInPolygon([NotNull] [ItemNotNull] List<WgsPoint> polygon, [NotNull] WgsPoint testPoint)

        {
            var result = false;
            var j = polygon.Count - 1;
            for (var i = 0; i < polygon.Count; i++) {
                if (polygon[i].Lat < testPoint.Lat && polygon[j].Lat >= testPoint.Lat || polygon[j].Lat < testPoint.Lat && polygon[i].Lat >= testPoint.Lat) {
                    if (polygon[i].Lon + (testPoint.Lat - polygon[i].Lat) / (polygon[j].Lat - polygon[i].Lat) * (polygon[j].Lon - polygon[i].Lon) < testPoint.Lon) {
                        result = !result;
                    }
                }

                j = i;
            }

            return result;
        }
    }
}