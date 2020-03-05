using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common;
using Common.Steps;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class MultiYearTendTest : UnitTestBase {
        public MultiYearTendTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunMultiyearTest()
        {
            PrepareUnitTest();
            string fn = WorkingDirectory.Combine("trend.xlsx");
            Config.LimitToScenarios.Clear();
            Config.InitializeSlices(Logger);
            MultiyearTrend trend = new MultiyearTrend();
            Random rnd = new Random();
            if (Config.Slices == null) {
                throw new FlaException("No slices");
            }
            trend[Constants.PresentSlice].AddValue("MyValue", 50, DisplayUnit.Stk);
            foreach (var slice in Config.Slices) {
                trend[slice].AddValue("MyValue", rnd.Next(100), DisplayUnit.Stk);
            }

            XlsxDumper.DumpMultiyearTrendToExcel(fn, trend);
            Info("Wrote to " + fn);
            Process.Start(fn);
        }
        [Fact]
        public void RunMultiyearTestSkippedValues()
        {
            PrepareUnitTest();
            string fn = WorkingDirectory.Combine("trend2.xlsx");
            Config.LimitToScenarios.Clear();
            Config.InitializeSlices(Logger);
            MultiyearTrend trend = new MultiyearTrend();
            Random rnd = new Random();
            if (Config.Slices == null) {
                throw new FlaException("No slices");
            }
            trend[Constants.PresentSlice].AddValue("MyValue", 50, DisplayUnit.Stk);
            foreach (var slice in Config.Slices) {
                if (slice.DstScenario == Scenario.FromEnum(ScenarioEnum.Nep)) {
                    continue;
                }

                if (slice.DstYear == 2030) {
                    continue;
                }
                trend[slice].AddValue("MyValue", rnd.Next(100), DisplayUnit.Stk);
            }

            XlsxDumper.DumpMultiyearTrendToExcel(fn, trend);
            Info("Wrote to " + fn);
            Process.Start(fn);
        }

        [Fact]
        public void RunMultiyearTestSingleColumn()
        {
            PrepareUnitTest();
            string fn = WorkingDirectory.Combine("trend3.xlsx");
            Config.LimitToScenarios.Clear();
            Config.InitializeSlices(Logger);
            MultiyearTrend trend = new MultiyearTrend();
            Random rnd = new Random();
            if (Config.Slices == null) {
                throw new FlaException("No slices");
            }
            trend[Constants.PresentSlice].AddValue("MyValue", 50, DisplayUnit.Stk);
            foreach (var slice in Config.Slices) {
                if (slice.DstScenario != Scenario.FromEnum(ScenarioEnum.Nep)) {
                    continue;
                }

                trend[slice].AddValue("MyValue", rnd.Next(100), DisplayUnit.Stk);
            }

            XlsxDumper.DumpMultiyearTrendToExcel(fn, trend);
            Info("Wrote to " + fn);
            Process.Start(fn);
        }
    }

    public class MultiyearTrend {
        [NotNull]
        public Dictionary<ScenarioSliceParameters, SliceSingleValueStore> Dict { get; } = new Dictionary<ScenarioSliceParameters, SliceSingleValueStore>();

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        [NotNull]
        public SliceSingleValueStore this[[NotNull] ScenarioSliceParameters slice] {
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
            get {
                if (!Dict.ContainsKey(slice)) {
                    Dict.Add(slice, new SliceSingleValueStore(slice));
                }

                return Dict[slice];
            }
        }
    }
}
