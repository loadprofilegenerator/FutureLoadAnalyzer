using System;
using System.Diagnostics;
using Common;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {

    public class MultiYearMultivariableTrendTest : UnitTestBase {
        public MultiYearMultivariableTrendTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunMultiyearTest()
        {
            PrepareUnitTest();
            string fn = WorkingDirectory.Combine("trend.xlsx");
            Config.LimitToScenarios.Clear();
            Config.InitializeSlices(Logger);
            MultiyearMultiVariableTrend trend = new MultiyearMultiVariableTrend();
            Random rnd = new Random();
            if (Config.Slices == null) {
                throw new FlaException("No slices");
            }
            trend[Constants.PresentSlice].AddValue("MyValue", "mycategory", 50, DisplayUnit.Stk);
            trend[Constants.PresentSlice].AddValue("MyValue", "mycategory2", 50, DisplayUnit.Stk);
            foreach (var slice in Config.Slices) {
                Info("Slice " + slice);
                trend[slice].AddValue("MyValue", "mycategory", rnd.Next(100), DisplayUnit.Stk);
                trend[slice].AddValue("MyValue", "mycategory2", rnd.Next(100), DisplayUnit.Stk);
            }

            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(fn, trend);
            Info("Wrote to " + fn);
            Process.Start(fn);
        }
    }
}
