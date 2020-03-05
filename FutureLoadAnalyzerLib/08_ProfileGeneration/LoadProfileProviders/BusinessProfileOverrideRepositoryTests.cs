using Common.Config;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class BusinessProfileOverrideRepositoryTests : UnitTestBase {
        public BusinessProfileOverrideRepositoryTests([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunTest()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            BusinessProfileOverrideRepository bpoe = new BusinessProfileOverrideRepository(rc);
            bpoe.GetEntry("blub", "blub", "blub").Should().BeNull();
            bpoe.GetEntry("bla", "bla", "bla").Should().NotBeNull();
        }
    }
}