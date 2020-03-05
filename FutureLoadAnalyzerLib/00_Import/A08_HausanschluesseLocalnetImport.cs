using System.Data.SQLite;
using Common;
using Common.Steps;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using NPoco;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A08_HausanschluesseLocalnetImport : RunableWithBenchmark {
        public A08_HausanschluesseLocalnetImport([NotNull] ServiceRepository services)
            : base(nameof(A08_HausanschluesseLocalnetImport), Stage.Raw, 8, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            dbdst.RecreateTable<LocalnetHausanschlussImport>();
            string fn = CombineForRaw("hausanschluesse.sqlite");
            using (var dbsrc = new Database("Data Source="+fn, DatabaseType.SQLite, SQLiteFactory.Instance)) {
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