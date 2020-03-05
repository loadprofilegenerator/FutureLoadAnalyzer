using System.Collections.Generic;
using Data.Database;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling.Database {
    public class RowCollection {
        [NotNull]
        [ItemNotNull]
        public List<Row> Rows { get; } = new List<Row>();

        public void Add([NotNull] RowBuilder rowBuilder)
        {
            Rows.Add(rowBuilder.GetRow());
        }
    }
}