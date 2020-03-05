using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly {
    public class BoxplotTrace {
        public BoxplotTrace([CanBeNull] string name, [NotNull] List<double> x)
        {
            Name = name;
            X = x;
        }

        [JsonProperty("name")]
        [CanBeNull]
        public string Name { get; set; }
        [JsonProperty("type")]
        [NotNull]

        public string Type { get; set; } = "box";

        [JsonProperty("x")]
        [NotNull]
        public List<double> X { get; set; }

        [JsonProperty("boxpoints")]
        [DefaultValue(true)]
        public bool Boxpoints { get; set; }
    }
}