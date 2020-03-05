using System;
using System.Collections.Generic;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using JetBrains.Annotations;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A06_GwrAdressenImport : RunableWithBenchmark {
        public A06_GwrAdressenImport([NotNull] ServiceRepository services)
            : base(nameof(A06_GwrAdressenImport), Stage.Raw, 6, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\GWRAdressen.xlsx", 1, "A1", "W4000");

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[2, i + 1];
                if (o == null) {
                    throw new Exception("Value was null");
                }

                hdict.Add(o.ToString(), i + 1);
            }

            SqlConnection.RecreateTable<GwrAdresse>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;

            db.BeginTransaction();
            for (var row = 3; row < arr.GetLength(0); row++) {
                var a = new GwrAdresse();
                if (arr[row, hdict["EGID"]] == null) {
                    continue;
                }

                a.EidgGebaeudeidentifikator_EGID = Convert.ToInt32(arr[row, hdict["EGID"]]);
                a.EidgEingangsidentifikator_EDID = Helpers.GetInt(arr[row, hdict["EDID"]]);
                a.ErhebungsstelleBaustatistik_DESTNR = Helpers.GetInt(arr[row, hdict["DESTNR"]]);
                a.BauprojektIdLiefersystem_DBABID = (string)arr[row, hdict["DBABID"]];
                a.EingangsIdLiefersystem_DBADID = (string)arr[row, hdict["DBADID"]];
                a.BFSGemeindenummer_GGDENR = Helpers.GetInt(arr[row, hdict["GGDENR"]]);
                a.Gebaeudeeingangstatus_DSTAT = Helpers.GetInt(arr[row, hdict["DSTAT*"]]);
                a.Strassenbezeichnung_DSTR = (string)arr[row, hdict["DSTR"]];
                a.EingangsnummerGebaeude_DEINR = (string)arr[row, hdict["DEINR"]];
                a.AmtlicheStrassennummer_DSTRANR = Helpers.GetInt(arr[row, hdict["DSTRANR"]]);
                a.EidgStrassenidentifikator_DSTRID = Helpers.GetInt(arr[row, hdict["DSTRID"]]);
                a.AmtlicherAdresscode_DADRC = Helpers.GetInt(arr[row, hdict["DADRC"]]);
                // ReSharper disable once PossibleInvalidOperationException
                a.Postleitzahl_DPLZ4 = (int)Helpers.GetDouble(arr[row, hdict["DPLZ4"]]);
                a.PLZZusatzziffer_DPLZZ = Helpers.GetInt(arr[row, hdict["DPLZZ"]]);
                a.EKoordinate_DKODE = Helpers.GetDouble(arr[row, hdict["DKODE"]]);
                a.NKoordinate_DKODN = Helpers.GetDouble(arr[row, hdict["DKODN"]]);
                a.XKoordinate_DKODX = Helpers.GetDouble(arr[row, hdict["DKODX"]]);
                a.YKoordinate_DKODY = Helpers.GetDouble(arr[row, hdict["DKODY"]]);
                a.Plausibilitaetsstatus_DPLAUS = Helpers.GetInt(arr[row, hdict["DPLAUS*"]]);
                // a.DatumderletztenAenderung_DMUTDAT = Helpers.GetDateTime(arr[row, hdict["DMUTDAT"]])
                // a.DatumdesExports_DEXPDAT = Helpers.GetDateTime(arr[row, hdict["DEXPDAT"]])
                // a. = (string)(arr[row, hdict[""]])
                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}