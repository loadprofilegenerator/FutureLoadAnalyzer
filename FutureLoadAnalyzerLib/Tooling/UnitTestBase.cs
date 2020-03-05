using Common.Config;
using Common.Logging;
using Common.Steps;
using Common.Testing;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib.Tooling {
    public class UnitTestBase{
        [CanBeNull]
        public ITestOutputHelper Output { get; }
        [NotNull]
        public RunningConfig Config { get; }
        [NotNull]
        public Logger Logger { get; }
        [NotNull]
        public WorkingDirectory WorkingDirectory { get;  set; }
        // ReSharper disable once NotNullMemberIsNotInitialized
        public UnitTestBase([CanBeNull] ITestOutputHelper testOutputHelper)
        {
            Output = testOutputHelper;
            Config = RunningConfig.MakeDefaults();
            Logger = new Logger(Output,Config);
        }

        public void Info([NotNull] string message)
        {
            Logger.Info(message,Stage.Testing,  WorkingDirectory.GetCallingMethodAndClass());
        }

        /// <summary>
        /// separate function due to getting the right name of the calling function
        /// </summary>
        public void PrepareUnitTest()
        {
            WorkingDirectory = new WorkingDirectory(Logger, Config);
        }

        public void Cleanup()
        {
            //WorkingDirectory.CleanAndCreate();
        }
    }
}