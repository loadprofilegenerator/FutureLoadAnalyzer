using JetBrains.Annotations;

namespace Common.Steps {
    public class RGB {
        public RGB(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        [NotNull]
        public override string ToString() => "R:" + R + " G: " + G + " B:" + B;
    }
}