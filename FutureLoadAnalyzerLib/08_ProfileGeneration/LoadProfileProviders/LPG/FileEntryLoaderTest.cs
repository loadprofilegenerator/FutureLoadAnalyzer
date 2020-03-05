using System.Collections.Generic;
using Automation.ResultFiles;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class FileEntryLoaderTest :UnitTestBase {
        [Fact]
        public void LoadFileEntriesTest()
        {
            PrepareUnitTest();
            FileEntryLoader fel = new FileEntryLoader();
            string dir = WorkingDirectory.Combine(@"HouseholdLoadProfileProviderTests.InitializeTestCase\results\HouseJob.trafokreis\housename\Results.General.sqlite");
            fel.LoadFiles(dir);
            Assert.True(fel.Files.Count > 0);
            foreach (KeyValuePair<HouseholdKey, FileEntry> pair in fel.Files) {
                Logger.Info(pair.Value.FullFilename,Stage.Testing,nameof(FileEntryLoaderTest));
            }
        }

        public FileEntryLoaderTest([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}