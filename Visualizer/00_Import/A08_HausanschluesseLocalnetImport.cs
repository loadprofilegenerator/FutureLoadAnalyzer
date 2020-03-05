using System.Data.SQLite;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Src;
using JetBrains.Annotations;
using NPoco;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A08_HausanschluesseLocalnetImport : RunableWithBenchmark {
        public A08_HausanschluesseLocalnetImport([NotNull] ServiceRepository services)
            : base(nameof(A08_HausanschluesseLocalnetImport), Stage.Raw, 8, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<LocalnetHausanschlussImport>(Stage.Raw, Constants.PresentSlice);
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            using (var dbsrc = new Database("Data Source=U:\\SimZukunft\\RawDataForMerging\\hausanschluesse.sqlite", DatabaseType.SQLite, SQLiteFactory.Instance)) {
                dbdst.BeginTransaction();
                var anschluesse = dbsrc.Fetch<LocalnetHausanschlussImport>();
                foreach (var anschluss in anschluesse) {
                    dbdst.Insert(anschluss);
                }

                dbdst.CompleteTransaction();
            }
        }
    }
}