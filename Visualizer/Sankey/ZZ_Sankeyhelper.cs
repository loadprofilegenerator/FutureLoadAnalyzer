using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Common.Config;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer.Sankey {
    // ReSharper disable once InconsistentNaming
#pragma warning disable CA1707 // Identifiers should not contain underscores
    public class ZZ_Sankeyhelper : BasicLoggable {
#pragma warning restore CA1707 // Identifiers should not contain underscores

        public ZZ_Sankeyhelper([NotNull] ILogger logger, Stage myStage) : base(logger, myStage, nameof(ZZ_Sankeyhelper))
        {

        }

        public void Run([NotNull] SingleSankeyArrow arrow)
        {
            var fi = new FileInfo(arrow.FullPyFileName());
            using (var sw = new StreamWriter(fi.FullName)) {
                sw.WriteLine("import matplotlib.pyplot as plt");
                sw.WriteLine("from matplotlib.sankey import Sankey");
                sw.WriteLine("plt.rcParams[\"figure.figsize\"] = (16,10)");
                sw.WriteLine("sankey = Sankey(gap=" + arrow.TrunkLength / 2 + ")"); //head_angle = 180
                sw.WriteLine("# block A");
                sw.WriteLine("sankey.add(flows =[" + arrow.GetFlows() + "],");
                sw.WriteLine("orientations =[" + arrow.GetDirections() + "],");
                sw.WriteLine("labels =[" + arrow.GetNames() + "],");
                sw.WriteLine("pathlengths =[" + arrow.GetPathLengths() + "],");
                sw.WriteLine("trunklength = " + arrow.TrunkLength + ", alpha = 0.1)");

                sw.WriteLine("diagrams = sankey.finish()");
                sw.WriteLine("plt.box(False)");
                sw.WriteLine("plt.savefig('" + arrow.ArrowName + ".png')");
                sw.Close();
            }

            SankeyExecutor(arrow, fi);
        }

        public void Run([NotNull] [ItemNotNull] List<SingleSankeyArrow> arrows)
        {
            var fi = new FileInfo(arrows[0].FullPyFileName());
            using (var sw = new StreamWriter(fi.FullName)) {
                sw.WriteLine("import matplotlib.pyplot as plt");
                sw.WriteLine("from matplotlib.sankey import Sankey");
                sw.WriteLine("plt.rcParams[\"figure.figsize\"] = (16,10)");
                sw.WriteLine("sankey = Sankey(gap=" + arrows[0].TrunkLength / 2 + ")"); //head_angle = 180
                foreach (var arrow in arrows) {
                    sw.WriteLine("# block A");
                    sw.WriteLine("sankey.add(flows =[" + arrow.GetFlows() + "],");
                    sw.WriteLine("orientations =[" + arrow.GetDirections() + "],");
                    sw.WriteLine("labels =[" + arrow.GetNames() + "],");
                    sw.WriteLine("pathlengths =[" + arrow.GetPathLengths() + "],");
                    sw.WriteLine("trunklength = " + arrow.TrunkLength + ", alpha = 0.1)");
                }

                sw.WriteLine("diagrams = sankey.finish()");
                sw.WriteLine("plt.box(False)");
                sw.WriteLine("plt.savefig('" + arrows[0].ArrowName + ".png')");
                sw.Close();
            }

            SankeyExecutor(arrows[0], fi);
        }

        private void SankeyExecutor([NotNull] SingleSankeyArrow arrow, [NotNull] FileInfo fi)
        {
#pragma warning disable IDE0067 // Dispose objects before losing scope
#pragma warning disable CA2000 // Dispose objects before losing scope
            var process = new Process();
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore IDE0067 // Dispose objects before losing scope
            var startInfo = new ProcessStartInfo {
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = arrow.FullTargetDirectory(),
                FileName = "c:\\python37\\python.exe",
                Arguments = fi.Name,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.StartInfo = startInfo;
            process.Start();
            while (!process.StandardOutput.EndOfStream) {
                var line = process.StandardOutput.ReadLine();
                Info(line ?? throw new InvalidOperationException());
                // do something with line
            }

            while (!process.StandardError.EndOfStream) {
                var line = process.StandardError.ReadLine();
                Info(line ?? throw new InvalidOperationException());
                // do something with line
            }
        }
    }
}