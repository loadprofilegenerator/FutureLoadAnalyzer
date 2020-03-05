using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using MathNet.Numerics.Statistics;

namespace Data {
    public class BarSeriesEntry {
        public BarSeriesEntry([NotNull] string name) => Name = name;

        private BarSeriesEntry([NotNull] string name, double value, double col)
        {
            Name = name;
            for (var i = 0; i < col; i++) {
                Values.Add(0);
            }

            Values.Add(value);
        }


        [NotNull]
        public string Name { get; }

        [NotNull]
        public List<double> Values { get; } = new List<double>();

        /// <summary>
        ///     for making multi-colored charts where every column is different
        /// </summary>
        [NotNull]
        public static BarSeriesEntry MakeBarSeriesEntry([NotNull] string name, double valueToBeDisplayed, double numberOfTheColumn) =>
            new BarSeriesEntry(name, valueToBeDisplayed, numberOfTheColumn);

        [NotNull]
        public static BarSeriesEntry MakeBarSeriesEntry([NotNull] string name) =>
            new BarSeriesEntry(name);

        [NotNull]
        public static BarSeriesEntry MakeBarSeriesEntry([NotNull] Histogram histogram,
                                                        [ItemNotNull] [NotNull] out List<string> colNames,
                                                        [CanBeNull] string labelFormater = null)
        {
            var bs = new BarSeriesEntry("");
            colNames = new List<string>();
            for (var i = 0; i < histogram.BucketCount; i++) {
                bs.Values.Add(histogram[i].Count);
                if (labelFormater != null) {
                    colNames.Add(histogram[i].LowerBound.ToString(labelFormater, CultureInfo.InvariantCulture) + "-" +
                                 histogram[i].UpperBound.ToString(labelFormater, CultureInfo.InvariantCulture));
                }
                else {
                    colNames.Add(histogram[i].LowerBound + "-" + histogram[i].UpperBound);
                }
            }

            return bs;
        }

        [NotNull]
        public static BarSeriesEntry MakeBarSeriesEntryDividedBy1Mio([NotNull] string name, double value, double col) =>
            new BarSeriesEntry(name, value, col / 1000000);
    }
}