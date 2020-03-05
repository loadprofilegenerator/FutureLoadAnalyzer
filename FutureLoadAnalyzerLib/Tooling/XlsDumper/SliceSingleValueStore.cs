using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class SliceSingleValueStore {
        public SliceSingleValueStore([NotNull] ScenarioSliceParameters slice) => Slice = slice;

        [NotNull]
        public ScenarioSliceParameters Slice { get; }
        [NotNull]
        [ItemNotNull]
        public List<SingleSliceValue> Values { get; } = new List<SingleSliceValue>();

        public void AddValue([NotNull] string variable, [NotNull] object value, DisplayUnit unit)
        {
            if (Values.Any(x => x.VariableName == variable)) {
                throw new FlaException("Duplicate variable: " + variable);
            }

            switch (unit) {
                case DisplayUnit.Stk:
                    break;
                case DisplayUnit.GWh:
                    value = (double)value / 1_000_000.0;
                    break;
                case DisplayUnit.Mw:
                    value = (double)value / 1_000;
                    break;
                case DisplayUnit.Percentage:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
            Values.Add(new SingleSliceValue(variable, value, unit));
        }

        [CanBeNull]
        public object GetSliceValueByName([NotNull] string name)
        {
            return Values.FirstOrDefault(x => x.VariableName == name)?.Value;
        }
    }
}