using System.Collections.Generic;
using JetBrains.Annotations;

namespace Data
{
    public class LineSeriesEntry
    {
        public LineSeriesEntry([NotNull] string name) => Name = name;

        [NotNull]
        public string Name { get; }

        [NotNull]
        [ItemNotNull]
        public List<Point> Values { get; } = new List<Point>();
    }

    public class Point
    {
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        [NotNull]
        public override string ToString() => "X:" + X + ", Y: " + Y;
    }
}
