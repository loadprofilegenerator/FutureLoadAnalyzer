using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common.Steps;
using JetBrains.Annotations;

namespace Data.DataModel {
    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
    public class WgsPoint {
        private double _lat;
        private double _lon;

        [Obsolete("for json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public WgsPoint()
        {
        }

        public WgsPoint(double lon, double lat)
        {
            if (lon > 360) {
                throw new Exception("Non-converted point");
            }

            if (lat > 360) {
                throw new Exception("Non-converted point");
            }

            Lon = lon;
            Lat = lat;
        }

        public WgsPoint(double lon, double lat, [JetBrains.Annotations.NotNull] RGB rgb)
        {
            if (lon > 360) {
                throw new Exception("Non-converted point");
            }

            if (lat > 360) {
                throw new Exception("Non-converted point");
            }

            Lon = lon;
            Lat = lat;
            Rgb = rgb;
        }

        [CanBeNull]
        public string Label { get; set; }

        /// <summary>
        ///     x
        /// </summary>
        public double Lat {
            get => _lat;
            set {
                if (value > 360) {
                    throw new ArgumentOutOfRangeException(nameof(value), "Not converted, wrong koordinates");
                }

                if (value < 46 || value > 48) {
                    throw new ArgumentException("Not burgdorf");
                }

                _lat = value;
            }
        }

        /// <summary>
        ///     y
        /// </summary>
        public double Lon {
            get => _lon;
            set {
                if (value > 360) {
                    throw new ArgumentOutOfRangeException(nameof(value), "Not converted, wrong koordinates");
                }

                if (value < 6 || value > 8) {
                    throw new ArgumentException("Not burgdorf");
                }

                _lon = value;
            }
        }

        [CanBeNull]
        public RGB Rgb { get; set; }

        public int Size { get; set; } = 10;

        [JetBrains.Annotations.NotNull]
        public static WgsPoint ConvertKoordsToLonLat(double x, double y)
        {
            double y1;
            double x1;
            if ((x > 500000) & (x < 700000)) {
                y1 = (x - 600000) / 1000000;
                x1 = (y - 200000) / 1000000;
            }
            else if ((x > 2500000) & (x < 2700000)) {
                y1 = (x - 2600000) / 1000000;
                x1 = (y - 1200000) / 1000000;
            }
            else {
                throw new Exception("unknown koords");
            }

            var lon = 2.6779094 + 4.728982 * y1 + 0.791484 * y1 * x1 + 0.1306 * y1 * x1 * x1 - 0.0436 * y1 * y1 * y1;
            var lat = 16.9023892 + 3.238272 * x1 - 0.270978 * y1 * y1 - 0.002528 * x1 * x1 - 0.0447 * y1 * y1 * x1 - 0.0140 * x1 * x1 * x1;
            lon = lon * 100 / 36;
            lat = lat * 100 / 36;
            return new WgsPoint(lon, lat);
        }

        public double GetMinimumDistance([JetBrains.Annotations.NotNull] [ItemNotNull]
                                         List<WgsPoint> houseCoords)
        {
            if (houseCoords.Count == 0) {
                return double.MaxValue;
            }

            var distances = new List<double>();
            foreach (var point in houseCoords) {
                var distance = GetMinimumDistanceInMeters(point);
                distances.Add(distance);
            }

            return distances.Min();
        }

        public double GetMinimumDistanceInMeters([JetBrains.Annotations.NotNull] WgsPoint point)
        {
            const double earthRadius = 6371000; //meters
            var dLat = (point.Lat - _lat).ToRadians();
            var dLng = (point.Lon - _lon).ToRadians();
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(_lat.ToRadians()) * Math.Cos(point.Lat.ToRadians()) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var dist = (float)(earthRadius * c);

            return dist;
        }

        [NotNull]
        public override string ToString() => "Lat:" + _lat + " Lon:" + _lon;
    }
}