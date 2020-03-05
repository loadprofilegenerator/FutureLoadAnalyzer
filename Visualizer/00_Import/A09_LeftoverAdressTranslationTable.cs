using System;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Src;
using JetBrains.Annotations;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A09_LeftoverAdressTranslationTable : RunableWithBenchmark {
        public A09_LeftoverAdressTranslationTable([NotNull] ServiceRepository services)
            : base(nameof(A09_LeftoverAdressTranslationTable), Stage.Raw, 9, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\LeftoverAdressTranslationTable.xlsx", 1, "A1", "B400");
            SqlConnection.RecreateTable<AdressTranslationEntry>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;

            db.BeginTransaction();
            for (var row = 1; row < arr.GetLength(0); row++) {
                var a = new AdressTranslationEntry();
                if (arr[row, 1] == null) {
                    continue;
                }

                a.OriginalStandort = (string)arr[row, 1];
                if (arr[row, 2] == null) {
                    throw new Exception("dst adress was null");
                }

                a.TranslatedAdress = (string)arr[row, 2];
                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}