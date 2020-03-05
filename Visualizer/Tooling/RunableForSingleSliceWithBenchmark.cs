using System.Diagnostics;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Visualisation;
using Common.Steps;
using Data;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling {
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
            Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Starting " + Name, Name, MyStage, null));
            Info("Slice " + parameters.DstScenario + " - " + parameters.DstYear);
            var sw = new Stopwatch();
            sw.Start();
            RunActualProcess(parameters);
            if (Services.RunningConfig.MakeCharts) {
                var sw2 = new Stopwatch();
                Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Starting " + Name + " - Chartmaking", Name, MyStage, null));
                MakeChartFunctionExecuted = true;
                RunChartMaking(parameters);
                sw2.Stop();
                Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Finished " + Name + " - Chartmaking: " + Helpers.GetElapsedTimeString(sw2), Name, MyStage, null));
            }

            if (VisualizeSlice != null) {
                VisualizeSlice.MakeVisualization(parameters, Services, this);
            }

            LogCall(sw);
            sw.Stop();
            Services.MyLogger.AddMessage(new LogMessage(MessageType.Info, "Finished running " + Name + ": " + Helpers.GetElapsedTimeString(sw) + "Scenario " + parameters.DstScenario + " - " + parameters.DstYear, Name, MyStage, null));
        }

        protected virtual void RunChartMaking([NotNull] ScenarioSliceParameters parameters)
        {
            MakeChartFunctionExecuted = false;

        }

        protected abstract void RunActualProcess([NotNull] ScenarioSliceParameters parameters);
    }
}