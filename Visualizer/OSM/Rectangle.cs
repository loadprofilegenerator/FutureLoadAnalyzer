using System.Collections.Generic;
using Data.DataModel;
using JetBrains.Annotations;

namespace Visualizer.OSM {
    public class Rectangle {
        public Rectangle(double left, double top, double bottom, double right)
        {
            Left = left;
            Top = top;
            Bottom = bottom;
            Right = right;
        }

        public double Bottom { get; set; }

        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }

        public bool IsInside([NotNull] WgsPoint point)
        {
            if (point.Lon < Top && point.Lon > Bottom && point.Lat > Left && point.Lat < Right) {
                return true;
            }

            return false;
        }

        public bool IsOutside([NotNull] WgsPoint point)
        {
            if (point.Lon > Top || point.Lon < Bottom) {
                return true;
            }

            if (point.Lat < Left || point.Lat > Right) {
                return true;
            }

            return false;
        }

        [NotNull]
        [ItemNotNull]
        public List<OsmFeature> OsmFeaturesInRectangle { get; } = new List<OsmFeature>();
        public override string ToString() => "T:" + Top + " L" + Left + " B" + Bottom + " R" + Right;
    }
}