using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling {
    public class PngSvgConverter : BasicLoggable {

        private static void ClearFinishedProcesses([ItemNotNull] [NotNull] List<Process> processes)
        {
            Thread.Sleep(500);
            var exited = processes.Where(x => x.HasExited).ToList();
            foreach (Process exitedProcess in exited) {
                processes.Remove(exitedProcess);
            }
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void ConvertAllSVG( [NotNull] RunningConfig rc)
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
            DirectoryInfo di = new DirectoryInfo(rc.Directories.BaseProcessingDirectory);
            FileInfo[] files = di.GetFiles("*.svg", SearchOption.AllDirectories);
            List<Process> processes = new List<Process>();
            Info("#####################");
            Info("converting all the svg to png");
            Info("#####################");
            foreach (FileInfo fileInfo in files) {
                string dstfn = fileInfo.Name.Replace(".svg", ".png");
                bool makeNewFile = false;
                // ReSharper disable once AssignNullToNotNullAttribute
                FileInfo dstfi = new FileInfo(Path.Combine(fileInfo.DirectoryName, dstfn));
                if (!dstfi.Exists) {
                    makeNewFile = true;
                }
                else if (dstfi.LastWriteTimeUtc < fileInfo.LastWriteTimeUtc) {
                    makeNewFile = true;
                }

                if (makeNewFile) {
                    Info("Processing " + fileInfo.Name);
                    ProcessStartInfo psi = new ProcessStartInfo(@"c:\Program Files\Inkscape\inkscape.exe");
                    if(fileInfo.Directory == null) {
                        throw new FlaException( "fileInfo.Directory != null");
                    }

                    psi.WorkingDirectory = fileInfo.Directory.FullName;


                    psi.Arguments = fileInfo.Name + " -e " + dstfn + " --without-gui -b=#ffffff";
                    Process p = new Process {
                        StartInfo = psi
                    };
                    p.Start();
                    processes.Add(p);
                    while (processes.Count > 3) {
                        ClearFinishedProcesses(processes);
                    }
                }
                // ReSharper disable once RedundantIfElseBlock
                else {
                    // logger.Info("Skipping " + fileInfo.Name);
                }
            }

            while (processes.Count > 0) {
                ClearFinishedProcesses(processes);
            }

            Info("#####################");
            Info("finished converting all the svg to png");
            Info("#####################");
        }

        public PngSvgConverter([NotNull] ILogger logger, Stage myStage) : base(logger, myStage, nameof(PngSvgConverter))
        {
        }
    }
}
