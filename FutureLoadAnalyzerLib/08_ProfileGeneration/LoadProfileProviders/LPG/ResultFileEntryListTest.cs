using Xunit;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class ResultFileEntryListTest {
        [Fact]
        public void LoadResultFiles()
        {
            const string resultfile = @"V:\BurgdorfStatistics\unittests\HouseholdLoadProfileProviderTests.InitializeTestCase\results\HouseJob.trafokreis\housename";
            ResultFileEntryLoader.Load(resultfile);
        }
    }
}