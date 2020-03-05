using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib.Tooling {
    public class WeightedRandomAllocatorTester : UnitTestBase {
        public WeightedRandomAllocatorTester([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private class TestClass {
            public TestClass(int weight) => Weight = weight;

            public int Weight { get; }
            public override string ToString() => "W: " + Weight;
        }

        [Fact]
        public void RunTestForAllocatingToSum()
        {
            Random r = new Random();
            WeightedRandomAllocator<TestClass> tc = new WeightedRandomAllocator<TestClass>(r, Logger);
            List<TestClass> ts = new List<TestClass>();
            for (int i = 0; i < 100; i++) {
                ts.Add(new TestClass(r.Next(10)));
            }

            List<TestClass> pickedObjects = new List<TestClass>();
            const int totalrounds = 100;
            Action a = () => { tc.PickObjectUntilLimit(ts, x => x.Weight, x => x.Weight, 5000, true); };
            a.Should().Throw<FlaException>();
            for (int i = 0; i < totalrounds; i++) {
                var picked = tc.PickObjectUntilLimit(ts, (x => x.Weight), x => x.Weight, 200, false);
                double pickedSum = picked.Sum(x => x.Weight);
                pickedObjects.AddRange(picked);
                Info("sum: " + pickedSum + " object count: " + picked.Count);
                pickedSum.Should().BeGreaterOrEqualTo(50);
            }

            pickedObjects.Should().NotBeEmpty();
        }

        [Fact]
        public void RunTestForNumberOfObjects()
        {
            Random r = new Random();
            WeightedRandomAllocator<TestClass> tc = new WeightedRandomAllocator<TestClass>(r, Logger);
            List<TestClass> ts = new List<TestClass> {new TestClass(2), new TestClass(98)};
            List<TestClass> pickedObjects = new List<TestClass>();
            const int totalrounds = 1000000;
            for (int i = 0; i < totalrounds; i++) {
                var picked = tc.PickNumberOfObjects(ts, x => x.Weight, 1, true);
                if (picked.Count != 1) {
                    throw new Exception("Picking didn't work");
                }

                pickedObjects.Add(picked[0]);
            }

            if (pickedObjects.Count != totalrounds) {
                throw new Exception("Error, not enough picked objects.");
            }

            int ones = 0;
            int hundreds = 0;
            foreach (var testClass in pickedObjects) {
                if (testClass.Weight == 2) {
                    ones++;
                }
                else if (testClass.Weight == 98) {
                    hundreds++;
                }
                else {
                    throw new Exception("Unknown weight");
                }
            }

            Logger.Info("1:" + ones + " 98:" + hundreds, Stage.Testing, nameof(WeightedRandomAllocatorTester));
            Logger.Info("1:" + (ones / (double)totalrounds) * 100 + "% 100:" + (hundreds / (double)totalrounds * 100) + "%",
                Stage.Testing,
                nameof(WeightedRandomAllocatorTester));
        }
    }
}