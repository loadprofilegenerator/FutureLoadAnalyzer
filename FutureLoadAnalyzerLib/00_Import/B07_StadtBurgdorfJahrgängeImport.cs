using System;
using System.IO;
using Common;
using Common.Steps;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class B07_StadtBurgdorfJahrgängeImport : RunableWithBenchmark {
        public B07_StadtBurgdorfJahrgängeImport([NotNull] ServiceRepository services)
            : base(nameof(B07_StadtBurgdorfJahrgängeImport), Stage.Raw, 107, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<Jahrgang>();
            db.BeginTransaction();
            string fn = CombineForRaw("Jahrgänge.csv");
            using (var sr = new StreamReader(fn)) {
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