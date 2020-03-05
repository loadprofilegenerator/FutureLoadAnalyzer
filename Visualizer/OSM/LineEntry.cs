using Data.DataModel;
using JetBrains.Annotations;

namespace Visualizer.OSM {
    public class LineEntry {
        public LineEntry([NotNull] WgsPoint startPoint, [NotNull] WgsPoint endPoint, [NotNull] string text)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Text = text;
        }

        [NotNull]
        public string Text { get; set; }
        [NotNull]
        public WgsPoint StartPoint { get; set; }
        [NotNull]
        public WgsPoint EndPoint { get; set; }
    }
}