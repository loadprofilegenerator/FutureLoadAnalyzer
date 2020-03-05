using System.IO;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly {
    public class FlaPlotlyPlot  {
        public void RenderToFile([CanBeNull] object pData, [CanBeNull] object pLayout, [CanBeNull] object pConfig, [NotNull] string fileName)
        {
            var html = Render(pData, pLayout, pConfig);
            File.WriteAllText(fileName,html);
        }
        [NotNull]
        public string Render([CanBeNull] object pData, [CanBeNull] object pLayout, [CanBeNull] object pConfig)
        {
            object data = pData ?? new object();
            object layout = pLayout ?? new object();
            object config = pConfig ?? new object();

            string dataJson = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
            string layoutJson = JsonConvert.SerializeObject(layout, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
            string configJson = JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");


            sb.AppendLine("<html>");
            sb.AppendLine("<head><script src='https://cdn.plot.ly/plotly-latest.min.js'></script></head>");
            sb.AppendLine("<body><div id='plotContainer' style='width: 90%; height: 100%;'></div>");

            sb.AppendLine("<script>");
            sb.Append("data = ").Append(dataJson).AppendLine(";");
            sb.Append("layout = ").Append(layoutJson).AppendLine(";");
            sb.Append("config = ").Append(configJson).AppendLine(";");

            sb.AppendLine("var d3 = Plotly.d3;");
            sb.AppendLine("var img_png = d3.select('#pngexport');");
            sb.AppendLine("Plotly.newPlot('plotContainer', data, layout, config)");
            sb.AppendLine(" </script>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

    }
}