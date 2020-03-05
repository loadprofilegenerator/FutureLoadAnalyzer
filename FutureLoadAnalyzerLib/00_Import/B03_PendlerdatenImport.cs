using System;
using System.Collections.Generic;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class B03_PendlerdatenImport : RunableWithBenchmark {
        public B03_PendlerdatenImport([NotNull] ServiceRepository services)
            : base(nameof(B03_PendlerdatenImport), Stage.Raw, 103, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            ImportOutgoingCommuter();
            ImportIncomingCommuter();
        }

        private void ImportIncomingCommuter()
        {
            string fn = CombineForRaw("Pendlerdaten_arbeitsgemeinde_Burgdorf.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "K215", out var _);
            var headerToColumnDict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1); i++) {
                var o = arr[2, i ];

                if (o == null) {
                    throw new Exception("was null");
                }

                if (!headerToColumnDict.ContainsKey(o.ToString())) {
                    headerToColumnDict.Add(o.ToString(), i );
                }
            }

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<IncomingCommuter>();
            db.BeginTransaction();
            for (var row = 3; row < arr.GetLength(0); row++) {
                if (arr[row, headerToColumnDict["AO Kanton Nr."]] == null) {
                    continue;
                }

                var a = new IncomingCommuter();
                TransferFieldsIncoming(arr, headerToColumnDict, row, a);
                db.Save(a);
            }

            db.CompleteTransaction();
        }

        private void ImportOutgoingCommuter()
        {
            string fn = CombineForRaw("Pendlerdaten_wohngemeinde_Burgdorf.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "K115", out var _);
            var headerToColumnDict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) ; i++) {
                var o = arr[2, i];

                if (o == null) {
                    throw new Exception("was null");
                }

                if (!headerToColumnDict.ContainsKey(o.ToString())) {
                    headerToColumnDict.Add(o.ToString(), i);
                }
            }


            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<OutgoingCommuterSummary>();
            db.BeginTransaction();
            for (var row = 3; row < arr.GetLength(0); row++) {
                if (arr[row, headerToColumnDict["WG Kanton Nr."]] == null) {
                    continue;
                }

                var a = new OutgoingCommuterSummary();
                TransferFieldsOutgoing(arr, headerToColumnDict, row, a);
                db.Save(a);
            }

            db.CompleteTransaction();
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void TransferFieldsIncoming([NotNull] [ItemNotNull] object[,] arr, [NotNull] Dictionary<string, int> hdict, int row, [NotNull] IncomingCommuter a)
        {
            a.Wohngemeinde = Helpers.GetStringNotNull(arr[row, hdict["WO Gemeinde Name"]]);
            a.Wohnkanton = Helpers.GetStringNotNull(arr[row, hdict["WO Kanton Kürzel"]]);
            a.Entfernung = Helpers.GetDouble(arr[row, hdict["Entfernung"]]) ?? throw new Exception("Was null");
            a.Erwerbstätige = Helpers.GetInt(arr[row, hdict["Erwerbstätige"]]) ?? throw new Exception("was null");
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void TransferFieldsOutgoing([NotNull] [ItemNotNull] object[,] arr, [NotNull] Dictionary<string, int> hdict, int row, [NotNull] OutgoingCommuterSummary a)
        {
            a.Arbeitsgemeinde = Helpers.GetStringNotNull(arr[row, hdict["AO Gemeinde Name"]]);
            a.Arbeitskanton = Helpers.GetStringNotNull(arr[row, hdict["AO Kanton Kürzel"]]);
            a.Entfernung = Helpers.GetDouble(arr[row, hdict["Entfernung"]]) ?? throw new Exception("Was null");
            a.Erwerbstätige = Helpers.GetInt(arr[row, hdict["Erwerbstätige"]]) ?? throw new Exception("was null");
        }
    }
}