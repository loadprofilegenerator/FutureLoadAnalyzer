using System.Data.SQLite;
using BurgdorfStatistics.DataModel.Src;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using NPoco;
using Constants = Common.Constants;

namespace FutureLoadAnalyzerLib._00_Import {
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class A03_EnergieBedarfsDatenImport : RunableWithBenchmark {
        public A03_EnergieBedarfsDatenImport([NotNull] ServiceRepository services) : base(nameof(A03_EnergieBedarfsDatenImport),
            Stage.Raw,
            3,
            services,
            true)
        {
        }

        protected override void RunActualProcess()
        {
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            dbdst.RecreateTable<EnergiebedarfsdatenBern>();
            string fn = CombineForRaw("energiebedarfsdatenBern.sqlite");
            using (var dbsrc = new Database("Data Source=" + fn, DatabaseType.SQLite, SQLiteFactory.Instance)) {
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