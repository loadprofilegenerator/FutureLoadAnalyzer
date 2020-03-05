using System.Collections.Generic;
using Common;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class MultiSliceValue {
        public MultiSliceValue([NotNull] string variableName,[NotNull] string category, [NotNull] object value, DisplayUnit unit)
        {
            VariableName = variableName;
            if (Values.ContainsKey(category)) {
                throw new FlaException("duplicate category");
            }
            Values.Add(category,value);
            Unit = unit;
        }

        [NotNull]
        public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        public DisplayUnit Unit { get; }

        [NotNull]
        public string VariableName { get; }

        [CanBeNull]
        public object GetValueByCategory([NotNull] string category)
        {
            if (!Values.ContainsKey(category)) {
                return null;
            }

            return Values[category];
        }
    }
}