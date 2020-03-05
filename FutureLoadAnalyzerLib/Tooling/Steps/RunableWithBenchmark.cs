using System.Diagnostics;
using Common;
using Common.Steps;
using Data;
using JetBrains.Annotations;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Tooling.Steps {
    public abstract class RunableWithBenchmark : BasicRunnable {
        protected RunableWithBenchmark([NotNull] string name,
                                       Stage stage,
                                       int sequenceNumber,
                                       [NotNull] ServiceRepository services,
                                       bool implementationFinished) : base(name,
            stage,
            sequenceNumber,
            Steptype.Global,
            services,
            implementationFinished,
            null)
        {
        }

        protected RunableWithBenchmark([NotNull] string name,
                                       Stage stage,
                                       int sequenceNumber,
                                       [NotNull] ServiceRepository services,
                                       bool implementationFinished,
                                       [NotNull] IVisualizeSlice visualizeSlice) : base(name,
            stage,
            sequenceNumber,
            Steptype.Global,
            services,
            implementationFinished,
            visualizeSlice)
        {
        }

        public void Run()
        {
            ClearTargetDirectory(Constants.PresentSlice);
            Debug("Starting " + Name);
            var sw = new Stopwatch();
            sw.Start();
            RunActualProcess();
            sw.Stop();

            if (Services.RunningConfig.MakeCharts) {
                var sw2 = new Stopwatch();
                Debug("Starting " + Name + " - Chartmaking");
                MakeChartFunctionExecuted = true;
                RunChartMaking();
                sw2.Stop();
                Debug("Finished " + Name + " - Chartmaking: " + Helpers.GetElapsedTimeString(sw2));
            }

            if (VisualizeSlice != null && Services.RunningConfig.MakeCharts) {
                VisualizeSlice.MakeVisualization(Constants.PresentSlice, this);
            }

            Info("Finished running " + Name + ": " + Helpers.GetElapsedTimeString(sw));
            LogCall(sw);
        }

        protected abstract void RunActualProcess();

        protected virtual void RunChartMaking()
        {
            MakeChartFunctionExecuted = false;
        }
    }
}