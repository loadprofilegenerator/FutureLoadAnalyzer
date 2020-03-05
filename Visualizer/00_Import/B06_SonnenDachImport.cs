using System.Data.SQLite;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using JetBrains.Annotations;
using NPoco;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class B06_SonnenDachImport : RunableWithBenchmark {
        public B06_SonnenDachImport([NotNull] ServiceRepository services)
            : base(nameof(B06_SonnenDachImport), Stage.Raw, 106, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<SonnenDach>(Stage.Raw, Constants.PresentSlice);
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            using (var dbsrc = new Database("Data Source=U:\\SimZukunft\\RawDataForMerging\\sonnendach.sqlite", DatabaseType.SQLite, SQLiteFactory.Instance)) {
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