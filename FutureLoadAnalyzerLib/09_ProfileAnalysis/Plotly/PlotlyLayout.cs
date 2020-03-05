using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly {
    public class PlotlyLayout {
        [JsonProperty("title")]
        [CanBeNull]
        public string Title { get; set; }
        [JsonProperty("height")]
        [CanBeNull]
        public int? Height { get; set; }
        [JsonProperty("margin")]
        [CanBeNull]
        public Margin Margin { get; set; }

        [JsonProperty("showlegend")]
        [DefaultValue(true)]
        public bool ShowLegend { get; set; }
    }
}