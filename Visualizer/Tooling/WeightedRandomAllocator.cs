using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BurgdorfStatistics.Tooling {

    public class WeightedRandomAllocatorTester {
        [NotNull] private readonly ITestOutputHelper _to;
        public WeightedRandomAllocatorTester([NotNull] ITestOutputHelper to)
        {
            this._to = to;
        }
        private class TestClass {
            public TestClass(int weight) => Weight = weight;

            public int Weight { get; }
            public override string ToString() => "W: " + Weight;
        }
        [Fact]
        public void RunTest()
        {
            Random r = new Random();
            WeightedRandomAllocator<TestClass> tc = new WeightedRandomAllocator<TestClass>(r);
            List<TestClass> ts = new List<TestClass> {new TestClass(2), new TestClass(98)};
            List<TestClass> pickedObjects = new List<TestClass>();
            int totalrounds = 1000000;
            for (int i = 0; i < totalrounds; i++) {
                var picked = tc.PickObjects(ts, (x => x.Weight), 1);
                if(picked.Count != 1) {
                    throw new Exception("Picking didn't work");
                }

                pickedObjects.Add(picked[0]);
            }

            if (pickedObjects.Count != totalrounds) {
                throw new Exception("Error, not enough picked objects.");
            }
            int ones = 0;
            int hundreds =0;
            foreach (var testClass in pickedObjects) {
                if (testClass.Weight == 2) {
                    ones++;
                }else if (testClass.Weight == 98) {
                    hundreds++;
                }
                else {
                    throw new Exception("Unknown weight");
                }
            }
            _to.WriteLine("1:" + ones + " 98:" + hundreds);
            _to.WriteLine("1:" + (ones/(double)totalrounds)*100 + "% 100:" + (hundreds/(double)totalrounds *100) + "%");
        }
    }
    public class WeightedRandomAllocator<T> {
        [NotNull] Random rnd ;

        public WeightedRandomAllocator([NotNull] Random rnd)
        {
            this.rnd = rnd;
        }

        public class Weight {
            public Weight([NotNull] T o, double weight, double cumulativeStart, double cumulativeEnd)
            {
                MyObject = o;
                MyWeight = weight;
                CumulativeStart = cumulativeStart;
                CumulativeEnd = cumulativeEnd;
            }

            [NotNull]
            public T MyObject { get;  }
            public double MyWeight { get;  }

            public double CumulativeStart { get;  }
            public double CumulativeEnd { get;  }

            public bool IsMatch(long value)
            {
                if (value >= CumulativeStart && value < CumulativeEnd) {
                    return true;
                }

                return false;
            }

            public override string ToString() => MyObject.ToString() + " (" + CumulativeStart + " - " + CumulativeEnd + ")";
        }

        [NotNull]
        [ItemNotNull]
        public List<T> PickObjectUntilLimit([NotNull][ItemNotNull] List<T> objects, [NotNull] Func<T, double> weighingFunction, [NotNull] Func<T, double> sumFunction, double sumToReach)
        {
            if (Math.Abs(sumToReach) < 0.0001)
            {
                return new List<T>();
            }
            double currentVal = 0;
            List<Weight> weights = new List<Weight>();
            foreach (var o in objects)
            {
                double thisWeight = weighingFunction(o);
                if (thisWeight > 0)
                {
                    Weight w = new Weight(o, thisWeight, currentVal, currentVal + thisWeight);
                    weights.Add(w);
                    currentVal += thisWeight;
                }
            }
            List<T> pickedObjects = new List<T>();
            int failures = 0;
            double currentSumFromPickedObjects = 0;
            while (currentSumFromPickedObjects < sumToReach && failures < objects.Count)
            {
                long nxt = (int)(rnd.NextDouble() * currentVal);

                var picked = weights.First(x => x.IsMatch(nxt));
                if (!pickedObjects.Contains(picked.MyObject))
                {
                    pickedObjects.Add(picked.MyObject);
                    currentSumFromPickedObjects += sumFunction(picked.MyObject);
                }
                else
                {
                    failures++;
                }
            }

            if (failures >= objects.Count)
            {
                throw new Exception("Too many failures while trying to pick objects of the type " + typeof(T).FullName);
            }
            return pickedObjects;
        }

        [NotNull]
        [ItemNotNull]
        public List<T> PickObjects([NotNull] [ItemNotNull] List<T> objects, [NotNull] Func<T, double> weighingFunction, int numberOfObjectsToPick)
        {
            if(numberOfObjectsToPick == 0)
            {
                return new List<T>();
            }
            double currentVal = 0;
            List<Weight> weights = new List<Weight>();
            foreach (var o in objects) {
                double thisWeight = weighingFunction(o);
                if (thisWeight > 0) {
                    Weight w = new Weight(o, thisWeight, currentVal, currentVal + thisWeight);
                    weights.Add(w);
                    currentVal += thisWeight;
                }
            }
            List<T> pickedObjects = new List<T>();
            int failures = 0;
            while (pickedObjects.Count < numberOfObjectsToPick && failures < objects.Count) {
                long nxt = (int) (rnd.NextDouble() * currentVal);

                var picked = weights.First(x => x.IsMatch(nxt));
                if (!pickedObjects.Contains(picked.MyObject)) {
                    pickedObjects.Add(picked.MyObject);
                }
                else {
                    failures++;
                }
            }

            if (failures >= objects.Count) {
                throw new Exception("Too many failures while trying to pick objects of the type " + typeof(T).FullName);
            }
            return pickedObjects;
        }
    }
}