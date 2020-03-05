using Data.DataModel;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib.Tooling.Map {
    public class MapTileTest : UnitTestBase {
        [Fact]
        public void TestIsInside()
        {
            MapTile mt = new MapTile {TopLon = 8, BottomLon = 7, LeftLat = 47, RightLat = 48};
            Assert.True(mt.IsInside(new WgsPoint(7.5, 47.5)));
        }

        public MapTileTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}