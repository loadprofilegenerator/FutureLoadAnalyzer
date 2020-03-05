using Common.Config;
using Common.Logging;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Visualizer.HtmlReport {
    public class ReportGeneratorBasicTester {
        public ReportGeneratorBasicTester([NotNull] ITestOutputHelper testOutputHelper) =>
            this._testOutputHelper = testOutputHelper;

        [NotNull] private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void Run()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            using (Logger logger = new Logger(_testOutputHelper, rc)) {
                ReportGenerator rg = new ReportGenerator(logger);
                rg.Run(rc);
            }
        }
    }
}