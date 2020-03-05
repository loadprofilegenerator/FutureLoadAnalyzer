using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class SingleSliceValue {
        public SingleSliceValue([NotNull] string variableName, [NotNull] object value, DisplayUnit unit)
        {
            VariableName = variableName;
            Value = value;
            Unit = unit;
        }

        [NotNull]
        public object Value { get; }

        public DisplayUnit Unit { get; }

        [NotNull]
        public string VariableName { get; }
    }
}