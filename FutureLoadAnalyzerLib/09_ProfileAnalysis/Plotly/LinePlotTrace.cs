using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly {
    public class LineplotTrace {
        public LineplotTrace([CanBeNull] string name, [NotNull] List<double> x, [NotNull] List<double> y)
        {
            Name = name;
            X = x;
            Y = y;
        }

        [JsonProperty("name")]
        [CanBeNull]
        public string Name { get; set; }
        [JsonProperty("type")]
        [NotNull]

        public string Type { get; set; } = "scatter";

        [JsonProperty("x")]
        [NotNull]
        public List<double> X { get; set; }
        [JsonProperty("y")]
        [NotNull]
        public List<double> Y { get; set; }

        [JsonProperty("boxpoints")]
        public bool Boxpoints { get; set; }
    }
}