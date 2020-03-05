using System;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    public class RndTest {
        public RndTest([CanBeNull] ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [CanBeNull] private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void RunGenderRandomizationTest()
        {
            Random rnd = new Random();
            int[] resultCounts = new int[2];
            for (int i = 0; i < 10000; i++) {
                int res = rnd.Next(2);
                resultCounts[res]++;
            }

            for (var i = 0; i < resultCounts.Length; i++) {
                var count = resultCounts[i];
                _testOutputHelper?.WriteLine(i + ": " + count);
            }
        }
    }
}