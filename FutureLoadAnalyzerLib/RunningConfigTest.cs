using Common.Config;
using Common.Steps;
using Common.Testing;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib {
    public class RunningConfigTest : UnitTestBase {
        public RunningConfigTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunSave()
        {
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Clear();
            settings.MyOptions.Add(Options.ReadFromExcel);
            settings.StagesToExecute.Clear();
            settings.StagesToExecute.Add(Stage.ScenarioCreation);
            settings.InitializeSlices(Logger);
            WorkingDirectory wd = new WorkingDirectory(Logger, settings);
            string path = wd.Combine("calcsettings.json");

            settings.SaveThis(path);
            RunningConfig loadedSettings = RunningConfig.Load(path);
            Assert.Equal(settings.MyOptions.Count, loadedSettings.MyOptions.Count);
            Assert.Equal(settings.StagesToExecute.Count, loadedSettings.StagesToExecute.Count);
        }
    }
}