using System;
using System.Collections.Generic;
using System.Linq;
using Common.Steps;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class SliceMultiValueStore {
        public SliceMultiValueStore([NotNull] ScenarioSliceParameters slice) => Slice = slice;

        [NotNull]
        public ScenarioSliceParameters Slice { get; }
        [NotNull]
        [ItemNotNull]
        public List<MultiSliceValue> Values { get; } = new List<MultiSliceValue>();

        public void AddValue([NotNull] string variable,[NotNull] string category, [NotNull] object value, DisplayUnit unit)
        {
            switch (unit) {
                case DisplayUnit.Stk:
                    break;
                case DisplayUnit.GWh:
                    value = (double)value / 1_000_000.0;
                    break;
                case DisplayUnit.Percentage:
                    break;
                case DisplayUnit.Mw:
                    value = (double)value / 1_000.0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }


            MultiSliceValue val = Values.FirstOrDefault(x => x.VariableName == variable);
            if (val != null) {
                val.Values.Add(category,value);
            }
            else
            {
                Values.Add(new MultiSliceValue(variable, category, value, unit));
            }
        }

        [CanBeNull]
        public object GetSliceValueByName([NotNull] string name, [NotNull] string category)
        {
            return Values.FirstOrDefault(x => x.VariableName == name)?.GetValueByCategory(category);
        }
    }
}