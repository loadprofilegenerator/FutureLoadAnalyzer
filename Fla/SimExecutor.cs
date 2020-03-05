using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Common;
using Common.Steps;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Fla {
    public class ExecutorTest {
        public ExecutorTest([NotNull] ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [NotNull] private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void Run()
        {
            SimExecutor ex = new SimExecutor(x => _testOutputHelper.WriteLine(x));
            List<StagesToExecute> stages = new List<StagesToExecute> {StagesToExecute.RawToHouses};
            List<Scenario> scenarios = new List<Scenario> {Scenario.FromEnum(ScenarioEnum.Present)};

            ex.Run(stages, scenarios, new List<int>());

            _testOutputHelper.WriteLine("###################");

            stages = new List<StagesToExecute> {StagesToExecute.RawToHouses, StagesToExecute.Scenarios, StagesToExecute.Profiles};
            scenarios = new List<Scenario> {Scenario.Present()};
            ex.Run(stages, scenarios, new List<int>());

            _testOutputHelper.WriteLine("###################");

            stages = new List<StagesToExecute> {StagesToExecute.RawToHouses, StagesToExecute.Scenarios, StagesToExecute.Profiles};
            scenarios = new List<Scenario> {Scenario.Present(), Scenario.FromEnum(ScenarioEnum.Utopia)};
            ex.Run(stages, scenarios, new List<int>());
        }
    }

    public class ExecutionThread {
        [NotNull] private readonly Action<string> _outputWriter;
        [CanBeNull] [UsedImplicitly] private Thread _myThread;

        public ExecutionThread([NotNull] string filename, [NotNull] Action<string> outputWriter)
        {
            _outputWriter = outputWriter;
            Filename = filename;
        }

        [CanBeNull]
        public Exception Exception { get; set; }

        [NotNull]
        public string Filename { get; }

        public bool IsFinished { get; set; }

        [NotNull]
        public static ExecutionThread Run([NotNull] string filename, [NotNull] Action<string> outputWriter)
        {
            ExecutionThread ex = new ExecutionThread(filename, outputWriter);
            Thread t = new Thread(() => ex.SafeRun(filename));
            t.Start();
            t.Name = filename;
            ex._myThread = t;
            return ex;
        }

        private void SafeRun([NotNull] string filename)
        {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "Fla.exe";
                if (!File.Exists(filename)) {
                    throw new FlaException("Missing file to execute: " + filename);
                }
                psi.Arguments = "run -filename " + filename;
                psi.WorkingDirectory = Environment.CurrentDirectory;
                psi.UseShellExecute = true;
                Process p = new Process();
                p.StartInfo = psi;
                _outputWriter("Starting of " + psi.Arguments + " @ " + DateTime.Now.ToLongTimeString() + ", elapsed: " + sw.Elapsed);
                p.Start();
                p.WaitForExit();
                IsFinished = true;
                _outputWriter("Finished " + psi.Arguments + " @ " + DateTime.Now.ToLongTimeString() + ", elapsed: " + sw.Elapsed);
                sw.Stop();
            }
            catch (Exception ex) {
                _outputWriter(ex.Message + "\n" + ex.StackTrace);
                Exception = ex;
            }
        }
    }

    public class SimExecutor {
        [NotNull] private readonly Action<string> _outputWriter;

        public SimExecutor([NotNull] Action<string> outputWriter) => _outputWriter = outputWriter;

        public void Run([NotNull] List<StagesToExecute> options, [NotNull] [ItemNotNull] List<Scenario> scenarios, [NotNull] List<int> years)
        {
            string optionStr = string.Join(",", options);
            string scenarioStr = "";
            if (scenarios.Count > 0) {
                scenarioStr = string.Join(",", scenarios);
            }

            string yearStr = "";
            if (years.Count > 0) {
                yearStr = string.Join(",", years);
            }

            _outputWriter("Stages:" + optionStr);
            _outputWriter("Scenarios: " + scenarioStr);
            _outputWriter("Years: " + yearStr);
            if (options.Contains(StagesToExecute.RawToHouses)) {
                const string fn = "Present-RawtoHousesRebuild.json";
                var thread = ExecutionThread.Run(fn, _outputWriter);
                List<ExecutionThread> threads = new List<ExecutionThread> {thread};
                WaitForFinishing(threads, "raw to houses", 0);
            }

            ScenarioGeneration(options, scenarios);
            ProfileGeneration(options, scenarios, years);
            //profilegeneration
        }

        [NotNull]
        [ItemNotNull]
        private static List<string> GetSuffixesForProfileGeneration([NotNull] Scenario scenario, [NotNull] List<int> years)
        {
            Console.WriteLine("Getting years for scenario " + scenario.Name);
            if (String.Equals(scenario.Name, Scenario.Present().Name, StringComparison.CurrentCultureIgnoreCase)) {
                return new List<string> {Scenario.Present().ToString()};
            }

            List<string> suffixes = new List<string>();
            for (int i = 2020; i <= 2050; i += 5) {
                if (years.Count > 0 && !years.Contains(i)) {
                    continue;
                }

                suffixes.Add(scenario + "-" + i);
            }

            return suffixes;
        }

        private void ProfileGeneration([NotNull] List<StagesToExecute> options,
                                       [NotNull] [ItemNotNull] List<Scenario> scenarios,
                                       [NotNull] List<int> years)
        {
            if (!options.Contains(StagesToExecute.Profiles)) {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            string stage = "Profile Generation";
            List<ExecutionThread> threads = new List<ExecutionThread>();
            foreach (var scenario in scenarios) {
                var suffixes = GetSuffixesForProfileGeneration(scenario, years);
                foreach (var suffix in suffixes) {
                    string fn = "ProfileGeneration-" + suffix + ".json";
                    var thread = ExecutionThread.Run(fn, _outputWriter);
                    threads.Add(thread);
                    WaitForFinishing(threads, stage, 17);
                }
            }

            WaitForFinishing(threads, stage, 0);
            stage = "CrossSliceProfileAnalysis";
            const string fnSvn = "CrossSliceProfileAnalysis-Present.json";
            var vishread = ExecutionThread.Run(fnSvn, _outputWriter);
            threads.Add(vishread);
            WaitForFinishing(threads, stage, 0);
            sw.Stop();
            _outputWriter("Total profiles execution time: " + sw.Elapsed);
        }

        private void ScenarioGeneration([NotNull] List<StagesToExecute> options, [NotNull] [ItemNotNull] List<Scenario> scenarios)
        {
            if (!options.Contains(StagesToExecute.Scenarios)) {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            var scenariosToGenerate = scenarios.Where(x => x != Scenario.Present());
            List<ExecutionThread> threads = new List<ExecutionThread>();
            foreach (var scenario in scenariosToGenerate) {
                if (scenario.Name.ToLower() == "present") {
                    continue;
                }
                string fn = "ScenarioCreation-" + scenario + ".json";
                var thread = ExecutionThread.Run(fn, _outputWriter);
                threads.Add(thread);
            }

            string stage = "ScenarioGeneration";
            WaitForFinishing(threads, stage, 0);

            stage = "ScenarioVisualisation";
            const string fnSvn = "ScenarioVisualisation-Present.json";
            var vishread = ExecutionThread.Run(fnSvn, _outputWriter);
            threads.Add(vishread);
            WaitForFinishing(threads, stage, 0);
            sw.Stop();
            _outputWriter("Total scenarios execution time: " + sw.Elapsed);
        }

        private void WaitForFinishing([NotNull] [ItemNotNull] List<ExecutionThread> threads, [NotNull] string stage, int targetCount)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int count = 0;
            while (threads.Count > targetCount) {
                count++;
                if (count == 30) {
                    _outputWriter("Working on " + stage + ", elapsed: " + sw.Elapsed + ", still running: " + threads.Count);
                    count = 0;
                }

                Thread.Sleep(1000);
                var finishedThreads = threads.Where(x => x.IsFinished).ToList();
                foreach (var finishedThread in finishedThreads) {
                    threads.Remove(finishedThread);
                    if (finishedThread.Exception != null) {
                        string s = "Thread execution failed: " + finishedThread.Filename + " error\n" + finishedThread.Exception.Message + "\n" +
                                   finishedThread.Exception.StackTrace;
                        Console.Write("#################################");
                        Console.Write("#################################");
                        Console.Write("#################################");
                        Console.Write(s);
                        Console.Write("#################################");
                        Console.Write("#################################");
                        Console.Write("#################################");

                        new Thread(new ThreadStart(delegate
                        {
                            MessageBox.Show
                            (
                                s,
                                "Error");
                        })).Start();
                        throw new FlaException();
                    }
                }
            }

            sw.Stop();
            _outputWriter("Total " + stage + " execution time: " + sw.Elapsed);
        }
    }
}