using System.Diagnostics;
using Common.Steps;
using Data;
using JetBrains.Annotations;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Tooling.Steps {
    public abstract class RunableForSingleSliceWithBenchmark : BasicRunnable {
        protected RunableForSingleSliceWithBenchmark([NotNull] string name, Stage stage, int sequenceNumber, [NotNull] ServiceRepository services, bool implementationFinished)
            : base(name, stage, sequenceNumber, Steptype.SliceProcessors, services, implementationFinished,null)
        {
        }

        protected RunableForSingleSliceWithBenchmark([NotNull] string name, Stage stage, int sequenceNumber, [NotNull] ServiceRepository services, bool implementationFinished, [CanBeNull] IVisualizeSlice sliceVisualizer)
            : base(name, stage, sequenceNumber, Steptype.SliceProcessors, services, implementationFinished, sliceVisualizer)
        {
        }

        public void RunForScenarios([NotNull] ScenarioSliceParameters parameters)
        {
            ClearTargetDirectory(parameters);
            Debug("Starting " + Name);
            Info("Slice " + parameters.DstScenario + " - " + parameters.DstYear);
            var sw = new Stopwatch();
            sw.Start();
            RunActualProcess(parameters);
            if (Services.RunningConfig.MakeCharts) {
                var sw2 = new Stopwatch();
                Debug( "Starting " + Name + " - Chartmaking");
                MakeChartFunctionExecuted = true;
                RunChartMaking(parameters);
                sw2.Stop();
                Debug("Finished " + Name + " - Chartmaking: " + Helpers.GetElapsedTimeString(sw2));
            }

            if (VisualizeSlice != null && Services.RunningConfig.MakeCharts) {
                var sw3 = new Stopwatch();
                Debug("Starting " + Name + " - visualization");
                // ReSharper disable once PossibleNullReferenceException
                VisualizeSlice.MakeVisualization(parameters,  this);
                sw3.Stop();
                Debug("Finished " + Name + " - visualization: " + Helpers.GetElapsedTimeString(sw3));
            }

            LogCall(sw);
            sw.Stop();
            Info("Finished " + Name + ": " + Helpers.GetElapsedTimeString(sw) + "Scenario " + parameters.DstScenario + " - " + parameters.DstYear);
        }

        protected virtual void RunChartMaking([NotNull] ScenarioSliceParameters slice)
        {
            MakeChartFunctionExecuted = false;

        }

        protected abstract void RunActualProcess([NotNull] ScenarioSliceParameters slice);
    }
}