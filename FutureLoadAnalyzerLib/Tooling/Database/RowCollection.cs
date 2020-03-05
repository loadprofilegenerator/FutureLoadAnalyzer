using System.Collections.Generic;
using Common;
using Data.Database;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.Database {
    public class RowCollection {
        public RowCollection([NotNull] string sheetName, [NotNull] string yAxisName)
        {
            if (sheetName.Length > 30) {
                throw new FlaException("RowLength > 30 chars, not allowed: " + sheetName + " was " + sheetName.Length);
            }
            SheetName = sheetName;
            YAxisName = yAxisName;
        }

        [NotNull]
        public string SheetName { get; }

        [NotNull]
        public string YAxisName { get; }

        [NotNull]
        [ItemNotNull]
        public List<Row> Rows { get; } = new List<Row>();

        [NotNull]
        [ItemNotNull]
        public List<string> ColumnsToSum { get; } = new List<string>();
        public double SumDivisionFactor { get; set; } = 1;

        public void Add([NotNull] RowBuilder rowBuilder)
        {
            Rows.Add(rowBuilder.GetRow());
        }
    }
}