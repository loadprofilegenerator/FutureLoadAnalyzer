using Xunit;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class HouseholdKeyEntryListTest {
        [Fact]
        public void LoadKeys()
        {
            const string resultfile = @"V:\BurgdorfStatistics\unittests\HouseholdLoadProfileProviderTests.InitializeTestCase\results\HouseJob.trafokreis\housename";
            HouseholdKeyEntryList.Load(resultfile);
        }
    }
}