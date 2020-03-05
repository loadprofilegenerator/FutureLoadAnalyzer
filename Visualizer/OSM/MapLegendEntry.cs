using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer.OSM {
    public class MapLegendEntry {
        [NotNull]
        public string Name { get; }

        public int R { get; }
        public int G { get; }
        public int B { get; }
        [NotNull]
        public RGB Rgb { get; }

        public override string ToString() => Name + ": " + Rgb;

        public MapLegendEntry([NotNull] string name, int r, int g, int b)
        {
            Name = name;
            R = r;
            G = g;
            B = b;
            Rgb = new RGB(r, g, b);
        }

        public MapLegendEntry([NotNull] string name, [NotNull] RGB color)
        {
            Name = name;
            R = color.R;
            G = color.G;
            B = color.B;
            Rgb = color;
        }
    }
}