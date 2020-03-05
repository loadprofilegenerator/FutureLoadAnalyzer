using System.Collections.Generic;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data;
using JetBrains.Annotations;
using Visualizer.OSM;
using Xunit;
using Xunit.Abstractions;

namespace Visualizer.Mapper {
    public class MapTester {
        public MapTester([NotNull] ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [NotNull] private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void Run()
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            using (var log = new Logger(_testOutputHelper, rc)) {
                var md = new MapDrawer(log, Stage.Testing);
                var points = new List<MapPoint> {
                    new MapPoint(10, 10, 0, 5),
                    new MapPoint(100, 100, 50, 5),
                    new MapPoint(200, 200, 100, 10),
                    new MapPoint(300, 300, 200, 15)
                };
                md.DrawMapSvg(points, @"c:\\work\\unittest\\map.svg", new List<MapLegendEntry>());
            }
        }
    }
}