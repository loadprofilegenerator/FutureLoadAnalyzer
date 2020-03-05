using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using FutureLoadAnalyzerLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PowerArgs;
using Visualizer.HtmlReport;

namespace Fla {
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    [UsedImplicitly]
    public class FlaProgram {
        [HelpHook]
        [ArgShortcut("-?")]
        [ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgActionMethod]
        [ArgDescription("Make a single example")]
        public void MakeConfig([NotNull] ExampleArgs ex)
        {
            string fn = ex.Filename;
            if (fn == null) {
                throw new FlaException("filename was null");
            }
            var rc = RunningConfig.MakeDefaults();
            rc.SaveThis(fn);
            Console.WriteLine("Finished writing the config file to " + fn + "");
        }

        [ArgActionMethod]
        [ArgDescription("RunSims")]
        [ArgShortcut("rs")]
        public void RunSims([NotNull] ExecutionArgs ex)
        {
            SimExecutor se = new SimExecutor((x) => Console.WriteLine(x));
            var scenarios = new List<Scenario>();
            foreach (var scenarioName in ex.Scenarios?? throw new FlaException("was null")) {
                scenarios.Add(Scenario.FromString(scenarioName));
            }
            se.Run(ex.Stages?? new List<StagesToExecute>(), scenarios, ex.Years ?? new List<int>());
        }
        [ArgActionMethod]
        [ArgDescription("Make a single example")]
        public void ResultReport([NotNull] RunArgument args)
        {
            if (!File.Exists(args.Filename)) {
                Console.WriteLine("No config found.");
            }
            if (args.Filename == null) {
                throw new FlaException("filename was null");
            }
            RunningConfig rconfig = RunningConfig.Load(args.Filename);
            Console.WriteLine("Starting...");
            Console.WriteLine("Config is:");
            Console.WriteLine(JsonConvert.SerializeObject(rconfig, Formatting.Indented));
            Console.WriteLine("----");
            Logger logger = new Logger(null,rconfig);
            ReportGenerator rgbt = new ReportGenerator(logger);
            rgbt.Run(rconfig);
        }


        [ArgActionMethod]
        [ArgDescription("Run one Settings file")]
        public void Run([NotNull] RunArgument args)
        {
            MainBurgdorfStatisticsCreator mb = new MainBurgdorfStatisticsCreator(null);
            if (!File.Exists(args.Filename)) {
                Console.WriteLine("No config found.");
            }

            if (args.Filename == null) {
                throw new FlaException("filename was null");
            }
            RunningConfig rconfig = RunningConfig.Load(args.Filename);
            Console.WriteLine("Starting...");
            Console.WriteLine("Config is:");
            Console.WriteLine(JsonConvert.SerializeObject(rconfig, Formatting.Indented));
            Console.WriteLine("----");
            Logger logger = new Logger(null, rconfig);
            logger.Info("initializing slices", Stage.Preparation,nameof(Program));
            rconfig.InitializeSlices(logger);
            rconfig.CheckInitalisation();
            mb.RunBasedOnSettings(rconfig, logger);
        }
    }
}