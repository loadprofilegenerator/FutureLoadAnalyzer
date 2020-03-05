using System;
using System.Collections.Generic;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A05_FeuerungsstaettenImport : RunableWithBenchmark {
        protected override void RunActualProcess()
        {
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\FeuerungsStätten.xlsx", 1, "A1", "P2400");

            var headerToColumns = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1] ?? "";
                headerToColumns.Add(o.ToString(), i + 1);
            }

            SqlConnection.RecreateTable<FeuerungsStaette>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;

            db.BeginTransaction();
            for (var row = 3; row < arr.GetLength(0); row++) {
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

        public A05_FeuerungsstaettenImport([NotNull] ServiceRepository services)
            : base(nameof(A05_FeuerungsstaettenImport), Stage.Raw, 5, services, true)
        {
        }
    }
}