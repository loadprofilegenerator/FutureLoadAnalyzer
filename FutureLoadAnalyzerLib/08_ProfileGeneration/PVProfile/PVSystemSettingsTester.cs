using System.IO;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public class PVSystemSettingsTester : UnitTestBase {
        public PVSystemSettingsTester([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunPVTest()
        {
            PVSystemKey key = new PVSystemKey(0, 30, 2050);
            PVSystemSettings pvs = new PVSystemSettings(key, 1, 1, Logger, 1);
            Directory.SetCurrentDirectory(Config.Directories.SamDirectory);
            var pvprofile = pvs.Run(Config);
            Logger.Info("Total Energy: " + pvprofile.EnergySum(), Stage.Testing, "RunPVTest");
        }
    }
}