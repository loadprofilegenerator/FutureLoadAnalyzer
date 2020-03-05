using System.Diagnostics;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Visualisation;
using Common;
using Common.Steps;
using Data;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling {
    public abstract class RunableWithBenchmark : BasicRunnable {
        public void Run()
        {
            Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Starting " + Name, Name, MyStage, null));
            var sw = new Stopwatch();
            sw.Start();
            RunActualProcess();
            sw.Stop();

            if (Services.RunningConfig.MakeCharts) {
                var sw2 = new Stopwatch();
                Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Starting " + Name + " - Chartmaking", Name, MyStage, null));
                MakeChartFunctionExecuted = true;
                RunChartMaking();
                sw2.Stop();
                Services.MyLogger.AddMessage(new LogMessage(MessageType.Info, "Finished " + Name + " - Chartmaking: " + Helpers.GetElapsedTimeString(sw2), Name, MyStage, null));
            }

            if (VisualizeSlice != null) {
                VisualizeSlice.MakeVisualization(Constants.PresentSlice, Services, this);
            }

            Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Finished running " + Name + ": " + Helpers.GetElapsedTimeString(sw), Name, MyStage, null));
            LogCall(sw);
        }

        protected abstract void RunActualProcess();

        protected virtual void RunChartMaking()
        {
            MakeChartFunctionExecuted = false;
        }

        protected RunableWithBenchmark([NotNull] string name, Stage stage, int sequenceNumber, [NotNull] ServiceRepository services, bool implementationFinished)
            : base(name, stage, sequenceNumber, Steptype.Global, services, implementationFinished, null)
        {
        }
        protected RunableWithBenchmark([NotNull] string name, Stage stage, int sequenceNumber, [NotNull] ServiceRepository services, bool implementationFinished,
                                       [NotNull] IVisualizeSlice visualizeSlice)
            : base(name, stage, sequenceNumber, Steptype.Global, services, implementationFinished, visualizeSlice)
        {
        }
    }
}