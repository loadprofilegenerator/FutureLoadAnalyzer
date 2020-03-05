using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Common;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class EnumWorksheetContent<TU> : IValueProvider {
        [NotNull] [ItemNotNull] private readonly List<string> _columnNames;
        [NotNull] [ItemNotNull] private readonly List<ReadOnlyCollection<TU>> _valuesList;

        public EnumWorksheetContent([NotNull] string sheetName,
                                    [NotNull] [ItemNotNull] List<string> columnNames,
                                    [NotNull] [ItemNotNull] List<ReadOnlyCollection<TU>> valuesList)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) {
                throw new FlaException("Sheetname was null");
            }

            _columnNames = columnNames;
            _valuesList = valuesList;

            SheetName = sheetName;
            if (columnNames.Count != _valuesList.Count) {
                throw new FlaException("invalid counts");
            }
        }

        public EnumWorksheetContent([NotNull] string sheetName,
                                    [NotNull] [ItemNotNull] List<string> columnNames,
                                    [NotNull] [ItemNotNull] params ReadOnlyCollection<TU>[] profiles)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) {
                throw new FlaException("Sheetname was null");
            }

            SheetName = sheetName;
            _columnNames = columnNames;
            _valuesList = new List<ReadOnlyCollection<TU>>(profiles);
            if (columnNames.Count != _valuesList.Count) {
                throw new FlaException("invalid counts");
            }
        }

        [CanBeNull]
        public double? ChartHeight { get; } = null;

        public int GetColumnCount() => _valuesList.Count;

        [NotNull]
        public List<string> GetColumnNames() => _columnNames;

        [CanBeNull]
        public string GetUnit { get; } = null;

        [NotNull]
        public ReadOnlyCollection<T> GetValues<T>(int column) =>
            _valuesList[column] as ReadOnlyCollection<T> ?? throw new InvalidOperationException();

        [NotNull]
        public Type ReturnType => typeof(TU);


        [NotNull]
        public string SheetName { get; }

        public int SpecialLineColumnIndex { get; } = -1;

        [NotNull]
        public string YAxisName { get; } = "";
    }
}