using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A01_BusinessNameImport : RunableWithBenchmark {
        public A01_BusinessNameImport([NotNull] ServiceRepository services)
            : base(nameof(A01_BusinessNameImport), Stage.Raw, 01, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            // ReSharper disable once StringLiteralTypo
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\BusinessInBurgdorf.xlsx", 1, "A1", "B2000");
            Services.SqlConnection.RecreateTable<BusinessName>(Stage.Raw, Constants.PresentSlice);

            var db = Services.SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            db.BeginTransaction();
            for (var row = 1; row < arr.GetLength(0); row++) {
                var a = new BusinessName();
                if (arr[row, 1] == null) {
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
            a.Name = Helpers.GetString(arr[row, 1]);
            a.Category = Helpers.GetStringNotNull(arr[row, 2]);
        }
    }
}