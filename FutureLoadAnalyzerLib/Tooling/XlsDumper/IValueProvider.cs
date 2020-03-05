using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FutureLoadAnalyzerLib.Tooling.Database;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public interface IRowCollectionProvider : IWorksheet {
        [NotNull]
        RowCollection RowCollection { get; }
    }

#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IWorksheet {
#pragma warning restore CA1040 // Avoid empty interfaces

    }
    public interface IValueProvider: IWorksheet {
        int GetColumnCount();
        [NotNull]
        string SheetName { get; }

        [NotNull]
        [ItemNotNull]
        ReadOnlyCollection<T> GetValues<T>(int column);

        [NotNull]
        [ItemNotNull]
        List<string> GetColumnNames();
        [NotNull]
        Type ReturnType { get; }

        int SpecialLineColumnIndex { get;  }

        [NotNull]
        string YAxisName { get; }
        [CanBeNull]
        string GetUnit { get; }
        [CanBeNull]
        double? ChartHeight { get; }
    }
}