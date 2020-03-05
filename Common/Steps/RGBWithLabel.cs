using JetBrains.Annotations;

namespace Common.Steps {
    public class RGBWithLabel {
        public RGBWithLabel([NotNull] RGB rgb, [NotNull] string label)
        {
            R = rgb.R;
            G = rgb.G;
            B = rgb.B;
            Label = label;
        }

        public RGBWithLabel(int r, int g, int b, [NotNull] string label)
        {
            R = r;
            G = g;
            B = b;
            Label = label;
        }

        public int B { get; set; }
        public int G { get; set; }

        [NotNull]
        public string Label { get; set; }

        public int R { get; set; }

        [NotNull]
        public RGB GetRGB() => new RGB(R, G, B);

        [NotNull]
        public override string ToString() => "R:" + R + " G: " + G + " B:" + B + ": " + Label;
    }
}