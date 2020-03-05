using System.Data.SQLite;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using JetBrains.Annotations;
using NPoco;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace BurgdorfStatistics._00_Import {
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class A03_EnergieBedarfsDatenImport : RunableWithBenchmark {
        public A03_EnergieBedarfsDatenImport([NotNull] ServiceRepository services)
            : base(nameof(A03_EnergieBedarfsDatenImport), Stage.Raw, 3, services, true)
        {
        }
        protected override void RunActualProcess()
        {
            Services.SqlConnection.RecreateTable<EnergiebedarfsdatenBern>(Stage.Raw, Constants.PresentSlice);
            var dbdst = Services.SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            using (var dbsrc = new Database("Data Source=U:\\SimZukunft\\RawDataForMerging\\energiebedarfsdatenBern.sqlite", DatabaseType.SQLite, SQLiteFactory.Instance)) {
                dbdst.BeginTransaction();
                var ebd = dbsrc.Fetch<EnergiebedarfsdatenBern>();
                foreach (var dataentry in ebd) {
                    dbdst.Insert(dataentry);
                }

                dbdst.CompleteTransaction();
            }
        }
    }
}