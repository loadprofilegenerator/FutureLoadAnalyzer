using JetBrains.Annotations;

namespace Visualizer.Sankey {
    public class SankeyEntry {
        public SankeyEntry([NotNull] string name, double value, double pathLength, Orientation orientation)
        {
            Name = name;
            Value = value;
            PathLength = pathLength;
            Orientation = orientation;
        }

        [NotNull]
        public string Name { get; set; }

        public double Value { get; set; }
        public double PathLength { get; set; }
        public Orientation Orientation { get; set; }
    }
}