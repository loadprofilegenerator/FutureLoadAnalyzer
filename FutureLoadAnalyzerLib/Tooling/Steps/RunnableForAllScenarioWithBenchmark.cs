using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.Steps {
    public abstract class RunnableForAllScenarioWithBenchmark : BasicRunnable {
        public void Run([NotNull] [ItemNotNull] List<ScenarioSliceParameters> slices, [NotNull] AnalysisRepository analysisRepo)
        {
            if (!slices.Any(x => x.Equals(Constants.PresentSlice))) {
                throw new FlaException("Missing present slice. Have: " + string.Join("\n", slices.Select(x => x.ToString())));
            }
            Info("Starting " + Name);
            var sw = new Stopwatch();
            sw.Start();
            MakeChartFunctionExecuted = false;
            RunActualProcess(slices, analysisRepo);
            if (Services.RunningConfig.MakeCharts) {
                var sw2 = new Stopwatch();
                Info("Starting " + Name + " - Chartmaking");
                MakeChartFunctionExecuted = true;
                RunChartMaking(slices);
                sw2.Stop();
                Info("Finished " + Name + " - Chartmaking: " + Helpers.GetElapsedTimeString(sw2));
            }
            sw.Stop();
            Info("Finished running " + Name + ": " + Helpers.GetElapsedTimeString(sw));
            LogCall(sw);
        }

        protected virtual void RunChartMaking([NotNull] [ItemNotNull] List<ScenarioSliceParameters> scenarios)
        {
            MakeChartFunctionExecuted = false;

        }
        protected abstract void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,  [NotNull] AnalysisRepository analysisRepo);

        protected RunnableForAllScenarioWithBenchmark([NotNull] string name, Stage stage, int sequenceNumber, [NotNull] ServiceRepository services, bool implementationFinished)
            : base(name, stage, sequenceNumber, Steptype.AllScenarios, services, implementationFinished, null)
        {
        }
    }
}