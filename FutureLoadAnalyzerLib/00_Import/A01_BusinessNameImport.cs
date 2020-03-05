using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A01_BusinessNameImport : RunableWithBenchmark {
        public A01_BusinessNameImport([NotNull] ServiceRepository services)
            : base(nameof(A01_BusinessNameImport), Stage.Raw, 01, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            // ReSharper disable once StringLiteralTypo
            ExcelHelper eh = new ExcelHelper(Services.Logger,MyStage);
            var arr = eh.ExtractDataFromExcel2( CombineForFlaSettings("BusinessInBurgdorf.xlsx"), 1, "A1", "C2000", out var _);
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<BusinessName>();

            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                var a = new BusinessName();
                if (arr[row, 0] == null) {
                    continue;
                }

                AssignFields(a, arr, row);
                db.Save(a);
            }

            db.CompleteTransaction();
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void AssignFields([NotNull] BusinessName a, [ItemNotNull] [NotNull] object[,] arr, int row)
        {
            a.Name = Helpers.GetString(arr[row, 0]);
            a.Category = Helpers.GetStringNotNull(arr[row, 2]);
            a.Standort = Helpers.GetString(arr[row, 1]);
        }
    }
}