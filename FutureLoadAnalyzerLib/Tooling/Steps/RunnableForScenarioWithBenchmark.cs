using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.Steps {
    public abstract class RunnableForScenarioWithBenchmark : BasicRunnable {
        protected RunnableForScenarioWithBenchmark([NotNull] string name,
                                                   Stage stage,
                                                   int sequenceNumber,
                                                   [NotNull] ServiceRepository services,
                                                   bool implementationFinished) : base(name,
            stage,
            sequenceNumber,
            Steptype.Scenario,
            services,
            implementationFinished,
            null)
        {
        }

        public void Run([NotNull] [ItemNotNull] List<ScenarioSliceParameters> slices)
        {
            ClearTargetDirectory(Constants.PresentSlice);
            if (!slices.Any(x => x.Equals(Constants.PresentSlice))) {
                throw new FlaException("Missing present slice. Have: " + string.Join("\n", slices.Select(x => x.ToString())));
            }

            Info("Starting " + Name);
            var sw = new Stopwatch();
            sw.Start();
            MakeChartFunctionExecuted = false;
            RunActualProcess(slices);
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

        protected abstract void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices);

        protected virtual void RunChartMaking([NotNull] [ItemNotNull] List<ScenarioSliceParameters> scenarios)
        {
            MakeChartFunctionExecuted = false;
        }
    }
}