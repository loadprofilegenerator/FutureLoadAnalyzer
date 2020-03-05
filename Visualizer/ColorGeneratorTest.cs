using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Visualizer {
    public class ColorGeneratorTest {
        public ColorGeneratorTest([NotNull] ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [NotNull] private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void Test()
        {
            for (var i = 0; i < 100; i++) {
                var r = ColorGenerator.GetRGB(i);
                _testOutputHelper.WriteLine(r.R + ", " + r.G + ", " + r.B);
            }
        }
    }
}