using System;
using System.Diagnostics.CodeAnalysis;
using Common;
using Data.DataModel;
using JetBrains.Annotations;

namespace Visualizer.OSM {
    public class OsmFeatureToPointDistance {
        public OsmFeatureToPointDistance([NotNull] WgsPoint wgsPoint, [NotNull] OsmFeature feature)
        {
            WgsPoint = wgsPoint;
            Feature = feature;
            Distance = CalcDistance();
        }

        public double Distance { get; }
        [NotNull]
        public OsmFeature Feature { get; }

        [NotNull]
        public WgsPoint WgsPoint { get; }

        private double CalcDistance()
        {
            var distance = double.MaxValue;
            if (Feature.WgsPoints.Count == 0) {
                throw new FlaException("Got a polygon without any wgs points");
            }

            for (var i = 0; i < Feature.WgsPoints.Count - 1; i++) {
                var point1 = Feature.WgsPoints[i];
                var point2 = Feature.WgsPoints[i + 1];
                var cd = FindDistanceToSegment(WgsPoint, point1, point2);
                if (distance > cd) {
                    distance = cd;
                }
            }

            return distance;
        }

        // Calculate the distance between
        // point pt and the segment p1 --> p2.
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static double FindDistanceToSegment([NotNull] WgsPoint pt, [NotNull] WgsPoint p1,
                                             [NotNull] WgsPoint p2) //, out PointF closest
        {
            var dx = p2.Lon - p1.Lon;
            var dy = p2.Lat - p1.Lat;
            if (dx == 0 && dy == 0) {
                // It's a point not a line segment.
                dx = pt.Lon - p1.Lon;
                dy = pt.Lat - p1.Lat;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            var t = ((pt.Lon - p1.Lon) * dx + (pt.Lat - p1.Lat) * dy) / (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0) {
                dx = pt.Lon - p1.Lon;
                dy = pt.Lat - p1.Lat;
            }
            else if (t > 1) {
                dx = pt.Lon - p2.Lon;
                dy = pt.Lat - p2.Lat;
            }
            else {
                var closest = new WgsPoint(p1.Lon + t * dx, p1.Lat + t * dy);
                dx = pt.Lon - closest.Lon;
                dy = pt.Lat - closest.Lat;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}