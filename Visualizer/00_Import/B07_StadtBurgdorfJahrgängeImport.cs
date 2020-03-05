using System;
using System.IO;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class B07_StadtBurgdorfJahrgängeImport : RunableWithBenchmark {
        public B07_StadtBurgdorfJahrgängeImport([NotNull] ServiceRepository services)
            : base(nameof(B07_StadtBurgdorfJahrgängeImport), Stage.Raw, 107, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<Jahrgang>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;

            db.BeginTransaction();
            using (var sr = new StreamReader(@"U:\SimZukunft\RawDataForMerging\Jahrgänge.csv")) {
                while (!sr.EndOfStream) {
                    var l = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(l)) {
                        var arr = l.Split(';');
                        var year = Convert.ToInt32(arr[0]);
                        var count = Convert.ToInt32(arr[1]);
                        var jg = new Jahrgang {
                            Jahr = year,
                            Count = count
                        };
                        db.Save(jg);
                    }
                }
            }

            db.CompleteTransaction();
        }
    }
}