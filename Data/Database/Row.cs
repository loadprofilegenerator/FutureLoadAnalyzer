using System.Collections.Generic;
using JetBrains.Annotations;

namespace Data.Database {
    public class Row {
        public Row([NotNull] [ItemNotNull] List<RowValue> values)
        {
            Values = values;
        }

        [NotNull]
        [ItemNotNull]
        public List<RowValue> Values { get; }

    }

    public enum ValueOrEquation {
        Value,
        Equation
    }
}