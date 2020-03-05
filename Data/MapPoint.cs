using System;
using System.Diagnostics.CodeAnalysis;
using Common;
using Data.DataModel;
using GeoJSON.Net.Geometry;
using JetBrains.Annotations;

namespace Data {
    public class MapPoint {
        public MapPoint([CanBeNull] double? x, [CanBeNull] double? y, double value, int radius)
        {
            Mode = MapPointMode.DotRelativeValue;
            X = x;
            Y = y;
            Value = value;
            Radius = radius;
        }

        public MapPoint([CanBeNull] double? x, [CanBeNull] double? y, int radius, int r, int g, int b)
        {
            Mode = MapPointMode.DotAbsoluteColor;
            X = x;
            X = x;
            Y = y;
            Radius = radius;
            R = r;
            G = g;
            B = b;
        }

        public MapPoint([NotNull] Polygon poly, int r, int g, int b)
        {
            Mode = MapPointMode.DotAbsoluteColor;
            GeoPoly = poly;
            R = r;
            G = g;
            B = b;
        }

        public int B { get; set; }
        public int G { get; set; }

        [CanBeNull]
        public Polygon GeoPoly { get; }

        public MapPointMode Mode { get; }
        public int R { get; set; }

        public int Radius { get; }

        public double Value { get; }

        [CanBeNull]
        public double? X { get; }

        [CanBeNull]
        public double? Y { get; }

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable IDE0060 // Remove unused parameter
        public int AdjustedX([CanBeNull] double? xmin, int width, double factor, bool invert = true)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1801 // Review unused parameters
        {
            // ReSharper disable once PossibleInvalidOperationException
            var d = X.Value;
            // ReSharper disable once PossibleInvalidOperationException
            return (int)((d - xmin) * factor);
        }

        public int AdjustedY([CanBeNull] double? ymin, int height, double factor, bool invert = true)
        {
            // ReSharper disable once PossibleInvalidOperationException
            var d = Y.Value;
            // ReSharper disable once PossibleInvalidOperationException
            if (invert) {
                return height - (int)((d - ymin) * factor);
            }

            // ReSharper disable once PossibleInvalidOperationException
            return (int)((d - ymin) * factor);
        }

        [NotNull]
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public MapPoint ConvertToSwissMapPoint()
        {
            double wgsX = GetValueInSexagesimal(Y.Value);
            double wgsY = GetValueInSexagesimal(X.Value);

            double phi = (wgsY - 169028.66) / 10000;
            double lambda = (wgsX - 26782.5) / 10000;
            double lv95E = 2600072.37 + 211455.93 * lambda - 10938.51 * lambda * phi - 0.36 * lambda * phi * phi - 44.54 * lambda * lambda * lambda;
            double lv95N = 1200147.07 + 308807.95 * phi + 3745.25 * lambda * lambda + 76.63 * phi * phi - 194.56 * lambda * lambda * phi +
                           119.79 * phi * phi * phi;
            double x = lv95E;
            double y = lv95N;
            if (Mode == MapPointMode.DotRelativeValue) {
                return new MapPoint(x, y, Value, Radius);
            }

            if (Mode == MapPointMode.DotAbsoluteColor) {
                return new MapPoint(x, y, Radius, R, G, B);
            }

            throw new FlaException("Unknown mappointmode");
        }

        public static double GetValueInSexagesimal(double d)
        {
            var degree = Math.Truncate(d);
            var minutesWithFrac = (d - Math.Truncate(d)) * 60;
            var minutes = Math.Truncate(minutesWithFrac);
            var seconds = (minutesWithFrac - Math.Truncate(minutesWithFrac)) * 60;
            return degree * 3600 + minutes * 60 + seconds;
        }
    }
}