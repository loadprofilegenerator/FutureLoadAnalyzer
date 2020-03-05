using System;
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
    public class A02_DirenIsnImport : RunableWithBenchmark {
        public A02_DirenIsnImport([NotNull] ServiceRepository services) : base(nameof(A02_DirenIsnImport), Stage.Raw, 02, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            string csvName = CombineForFlaSettings("EGID_ISN_InExports.xlsx");
            var p = new ExcelPackage(new FileInfo(csvName));
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<HausanschlussImport>();
            db.BeginTransaction();
            ExcelWorksheet ws = p.Workbook.Worksheets[1];
            int row = 2;
            while (ws.Cells[row, 1].Value != null) {
                var filename = ((string)ws.Cells[row, 1].Value).Replace(".xml", "");
                var objectid = (string)ws.Cells[row, 2].Value;
                var egid = Convert.ToInt32(ws.Cells[row, 3].Value);
                var isn = Convert.ToInt32(ws.Cells[row, 4].Value);
                var lon = Convert.ToDouble(ws.Cells[row, 5].Value);
                var lat = Convert.ToDouble(ws.Cells[row, 6].Value);
                var adress = (string)ws.Cells[row, 7].Value;
                if (lon > 360) {
                    throw new FlaException("Lon über 360°");
                }

                if (lat > 360) {
                    throw new FlaException("lat über 360°");
                }

                var o = new HausanschlussImport(filename, objectid, egid, isn, lon, lat, adress);
                db.Save(o);
                row++;
            }

            p.Dispose();
            db.CompleteTransaction();
        }
    }
}