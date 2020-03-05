using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly {
    public class Margin {
        [JsonProperty("l")]
        public int Left { get; set; }
        [JsonProperty("t")]
        public int Top { get; set; }
        [JsonProperty("b")]
        public int Bottom { get; set; }
        [JsonProperty("pad")]
        public int Padding { get; set; }
        [JsonProperty("autoexpand")]
        public bool Autoexpand { get; set; }
    }
}