using JetBrains.Annotations;

namespace Data.Database {
    public class RowValue {
        [NotNull]
        public string Name { get; }
        [CanBeNull]
        public object Value { get; set; }

        public RowValue([NotNull] string name, [CanBeNull] object value, ValueOrEquation valueOrEquation = ValueOrEquation.Value)
        {
            Name = name;
            Value = value;
            ValueOrEquation = valueOrEquation;
        }

        public ValueOrEquation ValueOrEquation { get; }
    }
}