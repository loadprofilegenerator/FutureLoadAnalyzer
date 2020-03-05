using System;
using Common;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingDegreeProfileTest : UnitTestBase {
        public CoolingDegreeProfileTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunTest()
        {
            Profile prof = Profile.MakeRandomProfile(new Random(), "myprofi", Profile.ProfileResolution.QuarterHour, -20, 40);
            CoolingDegreeProfile hdp = new CoolingDegreeProfile(prof, 25, 23);
            hdp.InitializeDailyAmounts(1000);
            Info("sum degree days: " + hdp.CalculateHeatingDegreeDaySum());
            Info("sum energy: " + hdp.CalculateYearlyConsumptionSum());
            foreach (var day in hdp.CoolingDegreeHours) {
                Info(day.ToString());
                if (day.DegreeHours < 0) {
                    throw new FlaException("Negative degree hours");
                }
            }

            hdp.CalculateYearlyConsumptionSum().Should().BeApproximately(1000, 1);
        }
    }
}