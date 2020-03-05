using System;
using Data;
using Data.DataModel;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Visualizer.Mapper {
    public class MapPointConverterTester {
        public MapPointConverterTester([CanBeNull] ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [CanBeNull] private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void Run()
        {
            _testOutputHelper?.WriteLine("2613730.237, 1212845.591");
            WgsPoint wgs = WgsPoint.ConvertKoordsToLonLat(2613730.237, 1212845.591);

            _testOutputHelper?.WriteLine("lat" + wgs.Lat + " long: " + wgs.Lon);
            {
                double d = wgs.Lon;
                var degree = Math.Truncate(d);
                var minutesWithFrac = (d - Math.Truncate(d)) * 60;
                var minutes = Math.Truncate(minutesWithFrac);
                var seconds = (minutesWithFrac - Math.Truncate(minutesWithFrac)) * 60;
                _testOutputHelper?.WriteLine(degree + " " + minutes + " " + seconds);
            }
            MapPoint mp1 = new MapPoint(wgs.Lat, wgs.Lon, 1, 1);
            var mp = mp1.ConvertToSwissMapPoint();
            _testOutputHelper?.WriteLine("x: " + mp.X + " y: " + mp.Y);
        }
    }
}