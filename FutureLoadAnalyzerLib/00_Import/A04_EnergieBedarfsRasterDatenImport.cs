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
    public class A04_EnergieBedarfsRasterDatenImport : RunableWithBenchmark {
        public A04_EnergieBedarfsRasterDatenImport([NotNull] ServiceRepository services)
            : base(nameof(A04_EnergieBedarfsRasterDatenImport), Stage.Raw, 4, services, true)
        {
        }


        protected override void RunActualProcess()
        {
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            dbdst.RecreateTable<RasterDatenEnergiebedarfKanton>();
            string fn = CombineForRaw("EnergieBedarfBern2016.sqlite");
            using (var dbsrc = new Database("Data Source=" +fn, DatabaseType.SQLite, SQLiteFactory.Instance)) {
                dbdst.BeginTransaction();
                var ebd = dbsrc.Fetch<RasterDatenEnergiebedarfKanton>();
                foreach (var dataentry in ebd) {
                    dbdst.Insert(dataentry);
                }

                dbdst.CompleteTransaction();
            }
        }
    }
}