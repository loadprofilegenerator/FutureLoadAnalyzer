using System.Data.SQLite;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using NPoco;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class B06_SonnenDachImport : RunableWithBenchmark {
        public B06_SonnenDachImport([NotNull] ServiceRepository services) : base(nameof(B06_SonnenDachImport), Stage.Raw, 106, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            dbdst.RecreateTable<SonnenDach>();
            string fn = CombineForRaw("sonnendach.sqlite");
            using (var dbsrc = new Database("Data Source=" + fn, DatabaseType.SQLite, SQLiteFactory.Instance)) {
                dbdst.BeginTransaction();
                var daecher = dbsrc.Fetch<SonnenDach>();
                foreach (var dach in daecher) {
                    dbdst.Insert(dach);
                }

                dbdst.CompleteTransaction();
            }
        }
    }
}