using System.IO;
using System.Threading;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics._10_ProfileExport {
    // ReSharper disable once InconsistentNaming
    public class Z12_CSVProfileCopier : RunableForSingleSliceWithBenchmark {

        private void CopyCSVFiles([NotNull] ScenarioSliceParameters parameters, int sequence , [NotNull] string exporteRName, [NotNull] string subPath)
        {
            var path = FilenameHelpers.GetTargetDirectory(Stage.ProfileExport, sequence, exporteRName, parameters);
            var di = new DirectoryInfo(Path.Combine(path, "Export"));
            if (!di.Exists) {
                throw new FlaException("Directory " + di.FullName + " does not exist");
            }
            var csvfiles = di.GetFiles("*.csv");
            if (csvfiles.Length == 0) {
                throw new FlaException("No exported files found");
            }

            string dstpath = @"v:\tstcopy\v01";
            dstpath = Path.Combine(dstpath, parameters.DstScenario.ToString(), parameters.DstYear.ToString(), subPath);
            if (Directory.Exists(dstpath))
            {
                Directory.Delete(dstpath, true);
                Thread.Sleep(500);
            }
            Directory.CreateDirectory(dstpath);
            Thread.Sleep(500);
            foreach (var fileInfo in csvfiles) {
                string dstfullName = Path.Combine(dstpath, fileInfo.Name);
                Info("Copying " + dstfullName);
                fileInfo.CopyTo(dstfullName);

            }

        }

        protected override void RunActualProcess(ScenarioSliceParameters parameters)
        {
            CopyCSVFiles(parameters, 900, nameof(Z09_CSVExporterGeneration), "Generation");
            CopyCSVFiles(parameters, 1000, nameof(Z10_CSVExporterLoad), "Load");
        }

        public Z12_CSVProfileCopier([NotNull] ServiceRepository services)
            : base(nameof(Z12_CSVProfileCopier), Stage.ProfileExport, 1200, services, false)
        {
        }
    }
}