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
    public class B04_PVAnlagenImport : RunableWithBenchmark {
        public B04_PVAnlagenImport([NotNull] ServiceRepository services) : base(nameof(B04_PVAnlagenImport), Stage.Raw, 104, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            string fn = CombineForRaw("2018.05.01_PVA-Anlagen_Daten.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "K205", out var _);

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1); i++) {
                var o = arr[0, i];
                if (o == null) {
                    throw new Exception("Value was null");
                }

                hdict.Add(o.ToString(), i);
            }


            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<LocalnetPVAnlage>();
            db.BeginTransaction();
            for (var row = 1; row < arr.GetLength(0); row++) {
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