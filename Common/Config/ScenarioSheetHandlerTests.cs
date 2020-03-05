using System.IO;
using System.Linq;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Common.Config {
    public class ScenarioSheetHandlerTests {
        public ScenarioSheetHandlerTests([NotNull] ITestOutputHelper output) => _output = output;

        [NotNull] private readonly ITestOutputHelper _output;

        [Fact]
        public void RunTest()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            using (var logger = new Logger(_output, rc)) {
                string path = Path.Combine(rc.Directories.BaseProcessingDirectory, "ScenarioDefinitions.xlsx");
                ScenarioSheetHandler ssh = new ScenarioSheetHandler(logger);
                var slices = ssh.GetData(path);
                var u2020 = slices.Single(x => x.DstScenario == ScenarioEnum.Utopia && x.DstYear == 2020);
                string s = JsonConvert.SerializeObject(u2020, Formatting.Indented);
                _output.WriteLine(s);
            }
        }
    }
}