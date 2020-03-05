namespace Common.Steps {
    public class RGBWithSize {
        public RGBWithSize(int r, int g, int b, int size)
        {
            R = r;
            G = g;
            B = b;
            Size = size;
        }

        public RGBWithSize([JetBrains.Annotations.NotNull] RGB rgb, int size)
        {
            R = rgb.R;
            G = rgb.G;
            B = rgb.B;
            Size = size;
        }

        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int Size { get; }
    }
}