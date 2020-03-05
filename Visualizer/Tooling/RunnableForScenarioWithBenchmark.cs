using System.Collections.Generic;
using System.Diagnostics;
using BurgdorfStatistics.Logging;
using Common.Steps;
using Data;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling {
    public abstract class RunnableForScenarioWithBenchmark : BasicRunnable {
        public void Run([NotNull] [ItemNotNull] List<ScenarioSliceParameters> scenarios)
        {
            Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Starting " + Name, Name, MyStage, null));
            var sw = new Stopwatch();
            sw.Start();
            MakeChartFunctionExecuted = false;
            RunActualProcess(scenarios);
            if (Services.RunningConfig.MakeCharts)
            {
                var sw2 = new Stopwatch();
                Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Starting " + Name + " - Chartmaking", Name, MyStage, null));
                MakeChartFunctionExecuted = true;
                RunChartMaking(scenarios);
                sw2.Stop();
                Services.MyLogger.AddMessage(new LogMessage(MessageType.Debug, "Finished " + Name + " - Chartmaking: " + Helpers.GetElapsedTimeString(sw2), Name, MyStage, null));
            }
            sw.Stop();
            Services.MyLogger.AddMessage(new LogMessage(MessageType.Info, "Finished running " + Name + ": " + Helpers.GetElapsedTimeString(sw)  , Name, MyStage, null));
            LogCall(sw);
        }

        protected virtual void RunChartMaking([NotNull] [ItemNotNull] List<ScenarioSliceParameters> scenarios)
        {
            MakeChartFunctionExecuted = false;

        }
        protected abstract void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices);

        protected RunnableForScenarioWithBenchmark([NotNull] string name, Stage stage, int sequenceNumber, [NotNull] ServiceRepository services, bool implementationFinished)
            : base(name, stage, sequenceNumber, Steptype.Scenario, services, implementationFinished,null)
        {
        }
    }
}