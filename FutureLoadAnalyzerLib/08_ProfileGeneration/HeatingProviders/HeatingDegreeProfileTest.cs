using System;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatingDegreeProfileTest : UnitTestBase {
        public HeatingDegreeProfileTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunTest()
        {
            Profile prof = Profile.MakeRandomProfile(new Random(), "myprofi", Profile.ProfileResolution.QuarterHour, -20, 40);
            HeatingDegreeProfile hdp = new HeatingDegreeProfile(prof, 15, 20);
            hdp.InitializeDailyAmounts(1000);
            Info("sum degree days: " + hdp.CalculateHeatingDegreeDaySum());
            Info("sum energy: " + hdp.CalculateYearlyConsumptionSum());
            foreach (var day in hdp.HeatingDegreeDays) {
                Info(day.ToString());
            }

            hdp.CalculateYearlyConsumptionSum().Should().BeApproximately(1000, 1);
        }
    }
}