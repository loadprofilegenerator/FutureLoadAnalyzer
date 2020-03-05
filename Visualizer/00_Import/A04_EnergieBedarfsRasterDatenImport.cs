using System.Data.SQLite;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using JetBrains.Annotations;
using NPoco;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A04_EnergieBedarfsRasterDatenImport : RunableWithBenchmark {
        public A04_EnergieBedarfsRasterDatenImport([NotNull] ServiceRepository services)
            : base(nameof(A04_EnergieBedarfsRasterDatenImport), Stage.Raw, 4, services, true)
        {
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<RasterDatenEnergiebedarfKanton>(Stage.Raw, Constants.PresentSlice);
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            using (var dbsrc = new Database("Data Source=U:\\SimZukunft\\RawDataForMerging\\EnergieBedarfBern2016.sqlite", DatabaseType.SQLite, SQLiteFactory.Instance)) {
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