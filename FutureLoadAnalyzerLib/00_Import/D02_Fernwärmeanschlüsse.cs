using System;
using System.IO;
using Common;
using Common.Steps;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class D02_Fernwärmeanschlüsse : RunableWithBenchmark {
        protected override void RunActualProcess()
        {
            string csvName = CombineForFlaSettings("Feiertage-Bern.xlsx");
            var p = new ExcelPackage(new FileInfo(csvName));
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<FeiertagImport>();
            db.BeginTransaction();
            ExcelWorksheet ws = p.Workbook.Worksheets[1];
            int row = 2;
            while (ws.Cells[row, 1].Value != null) {
                DateTime date = (DateTime)ws.Cells[row, 1].Value;
                string name = (string)ws.Cells[row, 2].Value;
                var o = new FeiertagImport(name,date);
                db.Save(o);
                row++;
            }
            p.Dispose();
            db.CompleteTransaction();
            var loadedFeiertage = db.Fetch<FeiertagImport>();
            loadedFeiertage.Should().HaveCount(row - 2);
        }

        public D02_Fernwärmeanschlüsse([NotNull] ServiceRepository services)
            : base(nameof(D02_Fernwärmeanschlüsse), Stage.Raw, 301, services, true)
        {
        }
    }
}