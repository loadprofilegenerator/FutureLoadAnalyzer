using System.IO;
using Common;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming

    // ReSharper disable once InconsistentNaming
    public class C08_WKWImport : RunableWithBenchmark {
        public C08_WKWImport([NotNull] ServiceRepository services) : base(nameof(C08_WKWImport), Stage.Raw, 208, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            string csvName = CombineForFlaSettings("Wasserkraftwerke.xlsx");
            var p = new ExcelPackage(new FileInfo(csvName));
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<WasserkraftwerkImport>();
            db.BeginTransaction();
            ExcelWorksheet ws = p.Workbook.Worksheets[1];
            int row = 2;
            while (ws.Cells[row, 1].Value != null) {
                string bezeichnung = (string)ws.Cells[row, 1].Value;
                string anlagennummer = (string)ws.Cells[row, 2].Value;
                string adresse = (string)ws.Cells[row, 3].Value;
                string status = (string)ws.Cells[row, 4].Value;
                string inbetriebnahme = ws.Cells[row, 5].Value?.ToString();
                var nennleistung = (double)ws.Cells[row, 6].Value;
                string standort = (string)ws.Cells[row, 7].Value;
                string complexName = (string)ws.Cells[row, 8].Value;
                string lastProfil = (string)ws.Cells[row, 9].Value;

                var o = new WasserkraftwerkImport(bezeichnung,
                    anlagennummer,
                    adresse,
                    status,
                    inbetriebnahme,
                    nennleistung,
                    standort,
                    complexName,
                    lastProfil);
                db.Save(o);
                row++;
            }

            p.Dispose();
            db.CompleteTransaction();
        }
    }
}