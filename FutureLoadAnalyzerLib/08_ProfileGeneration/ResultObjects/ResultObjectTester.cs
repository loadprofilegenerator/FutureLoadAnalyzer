using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects {
    public class ResultObjectTester : UnitTestBase {
        [Fact]
        public void Run()
        {
            PrepareUnitTest();
            ProfileGenerationRo ro = new ProfileGenerationRo();
            var resultFile = WorkingDirectory.Combine("TestFile.Tree.xlsx");
            Logger.Info("Dumping to " + resultFile, Stage.Testing,nameof(ResultObjectTester));
            ro.DumpToExcel(resultFile,XlsResultOutputMode.Tree);

            var resultFile2 = WorkingDirectory.Combine("TestFile.FullLine.xlsx");
            Logger.Info("Dumping to " + resultFile2, Stage.Testing, nameof(ResultObjectTester));
            ro.DumpToExcel(resultFile2, XlsResultOutputMode.FullLine);

            var resultFile3 = WorkingDirectory.Combine("TestFile.Trafo.xlsx");
            Logger.Info("Dumping to " + resultFile3, Stage.Testing, nameof(ResultObjectTester));
            ro.DumpToExcel(resultFile3, XlsResultOutputMode.ByTrafoStationTree);

        }

        public ResultObjectTester([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}