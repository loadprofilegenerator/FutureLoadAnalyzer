using Common;
using Common.Database;
using Common.Steps;
using Data.DataModel.Creation;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    public class AnalysisRepositoryTests : UnitTestBase {
        public AnalysisRepositoryTests([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void Run()
        {
            AnalysisRepository ar = new AnalysisRepository(Config);
            Config.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            Config.InitializeSlices(Logger);
            if (Config.Slices == null) {
                throw new FlaException("No slices");
            }

            Config.Slices.Should().HaveCountGreaterOrEqualTo(3);
            foreach (var slice in Config.Slices) {
                var house1 = ar.GetSlice(slice).Fetch<House>();
                var house2 = ar.GetSlice(slice).Fetch<House>();
                house1.Count.Should().Be(house2.Count);
            }
        }
    }
}