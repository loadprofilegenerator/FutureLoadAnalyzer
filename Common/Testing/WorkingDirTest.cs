using Common.Config;
using Common.Logging;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Common.Testing {
    public class WorkingDirTest {
        public WorkingDirTest([CanBeNull] ITestOutputHelper output) => _output = output;

        [CanBeNull] private readonly ITestOutputHelper _output;
        [Fact]
        public void RunWorkDirTest()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            using (Logger logger = new Logger(_output, rc)) {
                WorkingDirectory wd = new WorkingDirectory(logger, rc);
                _output?.WriteLine(wd.Dir);
            }
        }
    }
}