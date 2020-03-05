using Data.DataModel.Creation;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    public class HouseKeyHashTest {
        [NotNull] private readonly ITestOutputHelper _testOutputHelper;

        public HouseKeyHashTest([NotNull] ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void RunHouseKeyHashTest()
        {
            var key1 = Household.MakeHouseholdKey("complex", "standort", "hh");
            var key2 = Household.MakeHouseholdKey("complex", "standort", "hh");
            _testOutputHelper.WriteLine(key1);
            _testOutputHelper.WriteLine(key2);
            Assert.Equal(key1,key2);

        }
    }
}