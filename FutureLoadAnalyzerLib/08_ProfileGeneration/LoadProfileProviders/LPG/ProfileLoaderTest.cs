using Automation.ResultFiles;
using Common;
using Xunit;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class ProfileLoaderTest {

        [Fact]
        public void Run()
        {
            const string lpgDirectory = @"V:\BurgdorfStatistics\unittests\HouseholdLoadProfileProviderTests.InitializeTestCase\results\HouseJob.trafokreis\housename";
             var hkel = ResultFileEntryLoader.Load(lpgDirectory);
            var rfe= hkel.FindCorrectProfile(new HouseholdKey( "HH1"), "electricity");
            if (rfe == null) {
                throw new FlaException("no file found");
            }
            var p = ProfileLoader.LoadProfiles(rfe, lpgDirectory, out var _);
            Assert.True(p?.Values.Count > 0);
        }
    }
}