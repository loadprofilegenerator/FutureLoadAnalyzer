using System;
using Common.Steps;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Constants = Common.Constants;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A09_LeftoverAdressTranslationTable : RunableWithBenchmark {
        public A09_LeftoverAdressTranslationTable([NotNull] ServiceRepository services)
            : base(nameof(A09_LeftoverAdressTranslationTable), Stage.Raw, 9, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            string fn =  CombineForRaw("LeftoverAdressTranslationTable.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "B400", out var _);

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<AdressTranslationEntry>();
            db.BeginTransaction();
            for (var row = 0; row < arr.GetLength(0); row++) {
                var a = new AdressTranslationEntry();
                if (arr[row, 0] == null) {
                    continue;
                }

                a.OriginalStandort = (string)arr[row, 0];
                if (arr[row, 1] == null) {
                    throw new Exception("dst adress was null");
                }

                a.TranslatedAdress = (string)arr[row, 1];
                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}