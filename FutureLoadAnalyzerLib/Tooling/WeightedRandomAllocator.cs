using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling {
    public class WeightedRandomAllocator<T> {
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly Random _rnd;

        public WeightedRandomAllocator([NotNull] Random rnd, [NotNull] ILogger logger)
        {
            _rnd = rnd;
            _logger = logger;
        }

        [NotNull]
        [ItemNotNull]
        public List<T> PickNumberOfObjects([NotNull] [ItemNotNull] IReadOnlyCollection<T> objects,
                                           [NotNull] Func<T, double> weighingFunction,
                                           int numberOfObjectsToPick,
                                           bool failOnOversubscribe)
        {
            if (numberOfObjectsToPick == 0) {
                return new List<T>();
            }

            if (numberOfObjectsToPick >= objects.Count) {
                if (failOnOversubscribe) {
                    throw new FlaException("Trying to pick " + numberOfObjectsToPick + " from " + objects.Count);
                }

                return objects.ToList();
            }

            List<T> remainingObjects = objects.ToList();
            MakeWeightedArr(remainingObjects, weighingFunction, out var weights, out var upperWeighingBound);
            List<T> pickedObjects = new List<T>();
            while (pickedObjects.Count < numberOfObjectsToPick) {
                long nxt = (int)(_rnd.NextDouble() * upperWeighingBound);
                var picked = weights.First(x => x.IsMatch(nxt));
                remainingObjects.Remove(picked.MyObject);
                pickedObjects.Add(picked.MyObject);
                MakeWeightedArr(remainingObjects, weighingFunction, out weights, out upperWeighingBound);
            }

            return pickedObjects;
        }

        [NotNull]
        [ItemNotNull]
        public List<T> PickObjectUntilLimit([NotNull] [ItemNotNull] List<T> objects,
                                            [NotNull] Func<T, double> weighingFunction,
                                            [NotNull] Func<T, double> sumFunction,
                                            double sumToReach,
                                            bool failOnOversubscribe)
        {
            if (Math.Abs(sumToReach) < 0.0001) {
                return new List<T>();
            }

            double sumObjects = objects.Sum(x => sumFunction(x));
            if (sumObjects <= sumToReach) {
                if (failOnOversubscribe) {
                    throw new FlaException("trying to allocate more than all objects have");
                }

                _logger.Info("Oversubscribed. Returning all objects.", Stage.OtherWork, nameof(WeightedRandomAllocator<T>));
                return objects.ToList();
            }

            var objectsNotNull = objects.Where(x => sumFunction(x) > 0).ToList();
            var minimumValue = objectsNotNull.Min(x => sumFunction(x));
            if (sumToReach < minimumValue * 0.5) {
                throw new FlaException("Trying to allocate less than 50% of the smallest object has: Minium:" + minimumValue +
                                       " trying to allocate: " + sumToReach);
            }

            var objectsToPickFrom = objects.ToList();
            InitializeWeightedList(objectsToPickFrom, weighingFunction, out var objectsWithWeight, out var maxValueForRnd);
            List<T> pickedObjects = new List<T>();
            int failures = 0;
            double currentSumFromPickedObjects = 0;
            double overtolerance = 1.3;
            while (currentSumFromPickedObjects < sumToReach && pickedObjects.Count < objects.Count && failures < objects.Count * 100) {
                double d = _rnd.NextDouble() * maxValueForRnd;
                var picked = objectsWithWeight.First(x => x.IsMatch(d));
                if (currentSumFromPickedObjects + sumFunction(picked.MyObject) > sumToReach * overtolerance && objectsWithWeight.Count > 1) {
                    failures++;
                    overtolerance += 0.1;
                    continue;
                }

                objectsToPickFrom.Remove(picked.MyObject);
                InitializeWeightedList(objectsToPickFrom, weighingFunction, out objectsWithWeight, out maxValueForRnd);
                pickedObjects.Add(picked.MyObject);
                currentSumFromPickedObjects += sumFunction(picked.MyObject);
            }

            if (failures >= objects.Count) {
                throw new Exception("Too many failures while trying to pick objects of the type " + typeof(T).FullName);
            }

            return pickedObjects;
        }

        private static void InitializeWeightedList([NotNull] [ItemNotNull] List<T> objects,
                                                   [NotNull] Func<T, double> weighingFunction,
                                                   [NotNull] [ItemNotNull] out List<Weight> weights,
                                                   out double maximumValue)
        {
            double currentVal = 0;
            weights = new List<Weight>();
            foreach (var o in objects) {
                double thisWeight = weighingFunction(o);
                if (thisWeight > 0) {
                    Weight w = new Weight(o, currentVal, currentVal + thisWeight);
                    weights.Add(w);
                    currentVal += thisWeight;
                }
            }

            maximumValue = currentVal;
        }

        private static void MakeWeightedArr([NotNull] [ItemNotNull] IReadOnlyCollection<T> objects,
                                            [NotNull] Func<T, double> weighingFunction,
                                            [NotNull] [ItemNotNull] out List<Weight> weights,
                                            out double upperWeighingBound)
        {
            double currentVal = 0;
            weights = new List<Weight>();
            foreach (var o in objects) {
                double thisWeight = weighingFunction(o);
                if (thisWeight > 0) {
                    Weight w = new Weight(o, currentVal, currentVal + thisWeight);
                    weights.Add(w);
                    currentVal += thisWeight;
                }
            }

            upperWeighingBound = currentVal;
        }

        private class Weight {
            public Weight([NotNull] T o, double cumulativeStart, double cumulativeEnd)
            {
                MyObject = o;
                CumulativeStart = cumulativeStart;
                CumulativeEnd = cumulativeEnd;
            }

            public double CumulativeEnd { get; }

            public double CumulativeStart { get; }

            [NotNull]
            public T MyObject { get; }

            public bool IsMatch(double value)
            {
                if (value >= CumulativeStart && value < CumulativeEnd) {
                    return true;
                }

                return false;
            }

            public override string ToString() => MyObject + " (" + CumulativeStart + " - " + CumulativeEnd + ")";
        }
    }
}