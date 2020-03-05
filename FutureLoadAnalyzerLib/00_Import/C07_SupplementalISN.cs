using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming

    [UsedImplicitly]
    public class C07_SupplementalISN : RunableWithBenchmark {
        public C07_SupplementalISN([NotNull] ServiceRepository services) : base(nameof(C07_SupplementalISN), Stage.Raw, 207, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            string csvName = CombineForFlaSettings("SupplementalEgidIsn.xlsx");
            var p = new ExcelPackage(new FileInfo(csvName));
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<HausanschlussImportSupplement>();
            db.BeginTransaction();
            ExcelWorksheet ws = p.Workbook.Worksheets[1];
            int row = 2;
            List<string> addedStandorts = new List<string>();
            while (ws.Cells[row, 1].Value != null || ws.Cells[row, 2].Value != null) {
                var complex = (string)ws.Cells[row, 1].Value ?? throw new FlaException("complex was null");
                var standort = (string)ws.Cells[row, 2].Value;
                if (addedStandorts.Contains(standort)) {
                    throw new FlaException("Standort added twice: " + standort + " line: " + row);
                }

                addedStandorts.Add(standort);
                var targetisn = Convert.ToInt32(ws.Cells[row, 3].Value);
                var haFilename = ((string)ws.Cells[row, 4].Value)?.Replace(".xml", "");
                var haObjectid = (string)ws.Cells[row, 5].Value;
                var egid = Convert.ToInt32(ws.Cells[row, 6].Value);
                var isn = Convert.ToInt32(ws.Cells[row, 7].Value);
                var lon = Convert.ToDouble(ws.Cells[row, 8].Value);
                var lat = Convert.ToDouble(ws.Cells[row, 9].Value);
                var haadress = (string)ws.Cells[row, 10].Value;
                if (lon > 360) {
                    throw new FlaException("Lon über 360°");
                }

                if (lat > 360) {
                    throw new FlaException("lat über 360°");
                }

                var o = new HausanschlussImportSupplement(complex, standort, targetisn, haFilename, haObjectid, egid, isn, lon, lat, haadress);
                db.Save(o);
                row++;
            }

            if (row == 2) {
                throw new FlaException("Not a single row?");
            }

            p.Dispose();
            db.CompleteTransaction();
        }
    }
}