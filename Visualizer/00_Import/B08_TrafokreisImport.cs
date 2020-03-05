using System;
using System.Collections.Generic;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once InconsistentNaming
    public class B08_TrafoKreisImport : RunableWithBenchmark {
        // ReSharper disable once FunctionComplexityOverflow
        private static void TransferFields([NotNull] [ItemNotNull] object[,] arr, [NotNull] Dictionary<string, int> hdict, int row, [NotNull] TrafoKreisImport a)
        {
            a.U_STRASSE1 = Helpers.GetString(arr[row, hdict["U_STRASSE1"]]);
            a.U_STR_NR_I = Helpers.GetStringNotNull(arr[row, hdict["U_STR_NR_I"]]);
            a.DESCRIPTIO = Helpers.GetStringNotNull(arr[row, hdict["DESCRIPTIO"]]);
            a.U_EGID_ISE = Helpers.GetInt(arr[row, hdict["U_EGID_ISE"]]);
            a.U_OBJ_ID_I = Helpers.GetInt(arr[row, hdict["U_OBJ_ID_I"]]);
            a.U_TRAFOKRE = Helpers.GetStringNotNull(arr[row, hdict["U_TRAFOKRE"]]);
            a.HKOORD = Helpers.GetDouble(arr[row, hdict["HKOORD"]]) ?? throw new Exception("Value was null");
            a.VKOORD = Helpers.GetDouble(arr[row, hdict["VKOORD"]]) ?? throw new Exception("Value was null");
            a.u_Nr_Dez_E = Helpers.GetString(arr[row, hdict["u_Nr_Dez_E"]]);
        }

        public B08_TrafoKreisImport([NotNull] ServiceRepository services)
            : base(nameof(B08_TrafoKreisImport), Stage.Raw, 108, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\2019-02-27-b_EV_HA_pro_Trafokreis.xlsx", 1, "A1", "AL4000");
            var headerToColumnDict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1];

                if (o == null) {
                    throw new Exception("was null");
                }

                if (!headerToColumnDict.ContainsKey(o.ToString())) {
                    headerToColumnDict.Add(o.ToString(), i + 1);
                }
            }

            SqlConnection.RecreateTable<TrafoKreisImport>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                if (arr[row, headerToColumnDict["U_OBJ_ID_I"]] == null) {
                    continue;
                }

                var a = new TrafoKreisImport();
                TransferFields(arr, headerToColumnDict, row, a);
                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}