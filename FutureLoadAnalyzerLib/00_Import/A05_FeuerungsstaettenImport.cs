using System;
using System.Collections.Generic;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A05_FeuerungsstaettenImport : RunableWithBenchmark {
        public A05_FeuerungsstaettenImport([NotNull] ServiceRepository services) : base(nameof(A05_FeuerungsstaettenImport),
            Stage.Raw,
            5,
            services,
            true)
        {
        }

        protected override void RunActualProcess()
        {
            string fn = CombineForRaw("FeuerungsStätten.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "P2400", out var _);

            var headerToColumns = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[0, i] ?? "";
                headerToColumns.Add(o.ToString(), i);
            }

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<FeuerungsStaette>();
            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                var a = new FeuerungsStaette();
                if (arr[row, headerToColumns["EGID"]] == null) {
                    continue;
                }

                a.AnlageNr = Convert.ToInt32(arr[row, headerToColumns["AnlageNr"]]);
                a.AnlageStatus = (string)arr[row, headerToColumns["AnlageStatus"]];
                a.Strasse = (string)arr[row, headerToColumns["Strasse"]];
                var hausnummer = arr[row, headerToColumns["Hausnummer"]];
                if (hausnummer != null) {
                    a.Hausnummer = hausnummer.ToString();
                }

                a.PLZ = Helpers.GetInt(arr[row, headerToColumns["PLZ"]]);
                a.Ort = (string)arr[row, headerToColumns["Ort"]];
                a.EGID = Helpers.GetInt(arr[row, headerToColumns["EGID"]]);
                a.EDID = Helpers.GetInt(arr[row, headerToColumns["EDID"]]);
                a.XKoordinate = Helpers.GetInt(arr[row, headerToColumns["X-Koordinate"]]);
                a.YKoordinate = Helpers.GetInt(arr[row, headerToColumns["Y-Koordinate"]]);
                a.Gebäudeart = (string)arr[row, headerToColumns["Gebäudeart"]];
                a.Brennstoff = (string)arr[row, headerToColumns["Brennstoff"]];
                a.KesselBaujahr = Helpers.GetInt(arr[row, headerToColumns["KesselBaujahr"]]);
                a.KesselLeistung = Helpers.GetInt(arr[row, headerToColumns["KesselLeistung"]]);
                a.Energienutzung = (string)arr[row, headerToColumns["Energienutzung"]];
                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}