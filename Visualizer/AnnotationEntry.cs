using JetBrains.Annotations;

namespace Visualizer {
    public class AnnotationEntry {
        public AnnotationEntry([NotNull] string text, double x1, double y1, double direction, double yOffset)
        {
            Text = text;
            X1 = x1;
            Y1 = y1;
            Direction = direction;
            YOffset = yOffset;
        }

        public double Direction { get; }

        [NotNull]
        public string Text { get; }

        public double X1 { get; }
        public double Y1 { get; }
        public double YOffset { get; }
    }
}