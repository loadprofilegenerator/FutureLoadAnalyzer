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
    public class B04_PVAnlagenImport : RunableWithBenchmark {
        public B04_PVAnlagenImport([NotNull] ServiceRepository services)
            : base(nameof(B04_PVAnlagenImport), Stage.Raw, 104, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\2018.05.01_PVA-Anlagen_Daten.xlsx", 1, "A1", "K205");

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1];
                if (o == null) {
                    throw new Exception("Value was null");
                }

                hdict.Add(o.ToString(), i + 1);
            }

            SqlConnection.RecreateTable<LocalnetPVAnlage>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;

            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                var a = new LocalnetPVAnlage();
                if (arr[row, hdict["Bezeichnung"]] == null) {
                    continue;
                }

                a.Bezeichnung = Helpers.GetStringNotNull(arr[row, hdict["Bezeichnung"]]);
                a.Anlagenummer = Helpers.GetStringNotNull(arr[row, hdict["Anlagenummer"]]);
                a.Adresse = Helpers.GetStringNotNull(arr[row, hdict["Adresse"]]);
                a.Inbetriebnahme = Helpers.GetDateTime(arr[row, hdict["Inbetriebnahme (Jahr)"]]).Year.ToString();
                a.Leistungkwp = Helpers.GetNoNullDouble(arr[row, hdict["Solargenerator Leistung DC [kWp]"]]);
                a.HKoord = Helpers.GetNoNullDouble(arr[row, hdict["Solargenerator Leistung DC [kWp]"]]);
                a.VKoord = Helpers.GetNoNullDouble(arr[row, hdict["VKoord"]]);

                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}