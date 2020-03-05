using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Constants = Common.Constants;

namespace FutureLoadAnalyzerLib._00_Import {
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    // ReSharper disable once InconsistentNaming
    public class B01_LocalnetImport : RunableWithBenchmark {
        public B01_LocalnetImport([NotNull] ServiceRepository services) : base(nameof(B01_LocalnetImport), Stage.Raw, 101, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            // ReSharper disable once StringLiteralTypo
            string fn = CombineForRaw("reduced.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "AH476000", out var _);
            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[0, i];
                if (o == null) {
                    throw new Exception("was null");
                }

                hdict.Add(o.ToString(), i);
            }

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<Localnet>();
            db.BeginTransaction();
            int negativeIsnCounter = -1000;
            for (var row = 1; row < arr.GetLength(0); row++) {
                var a = new Localnet();
                if (arr[row, hdict["Termin"]] == null) {
                    continue;
                }

                AssignFields(a, arr, row, hdict, ref negativeIsnCounter);
                db.Save(a);
            }

            db.CompleteTransaction();
            var readVals = db.Fetch<Localnet>();
            foreach (Localnet localnet in readVals) {
                if (localnet.ObjektIDGebäude == -2146826246) {
                    throw new FlaException("-");
                }

                if (localnet.ObjektIDGebäude == null) {
                    throw new FlaException("was null");
                }
            }
        }

        protected override void RunChartMaking()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var localNet = dbRaw.Fetch<Localnet>();
            var sumPerVerrechnungstyp = new Dictionary<string, double>();
            var verrechnungstypen = localNet.Select(x => x.Verrechnungstyp).Distinct().ToList();
            var sumPerTarif = new Dictionary<string, double>();
            var tarife = localNet.Select(x => x.Tarif).Distinct().ToList();
            foreach (var s in verrechnungstypen) {
                sumPerVerrechnungstyp.Add(s, 0);
            }

            foreach (var s in tarife) {
                sumPerTarif.Add(s, 0);
            }

            foreach (var l in localNet) {
                //ignore all the entries that don't have a standort
                if (string.IsNullOrWhiteSpace(l.Objektstandort)) {
                    continue;
                }

                if (l.BasisVerbrauch != null && l.Verrechnungstyp != null) {
                    sumPerVerrechnungstyp[l.Verrechnungstyp] += l.BasisVerbrauch.Value;
                }

                if (l.BasisVerbrauch != null && l.Tarif != null) {
                    sumPerTarif[l.Tarif] += l.BasisVerbrauch.Value;
                }
            }

            foreach (var pair in sumPerVerrechnungstyp) {
                Debug("Verrechnungstyp:" + pair.Key + ": " + pair.Value);
            }

            foreach (var pair in sumPerTarif) {
                Debug("Tarif:" + pair.Key + ": " + pair.Value);
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void AssignFields([NotNull] Localnet a,
                                         [ItemNotNull] [NotNull] object[,] arr,
                                         int row,
                                         [NotNull] Dictionary<string, int> hdict,
                                         ref int negativeIsnCounter)
        {
            a.Termin = Helpers.GetDateTime(arr[row, hdict["Termin"]]).Ticks;
            a.TerminString = Helpers.GetDateTime(arr[row, hdict["Termin"]]).ToString(CultureInfo.InvariantCulture);

            a.TerminJahr = Helpers.GetInt(arr[row, hdict["Termin Jahr"]]);
            a.TerminSemester = Helpers.GetString(arr[row, hdict["Termin Semester"]]);
            a.TerminQuartal = Helpers.GetString(arr[row, hdict["Termin Quartal"]]);
            a.Basis = Helpers.GetDouble(arr[row, hdict["Basis"]]);
            a.BasisVerbrauch = Helpers.GetDouble(arr[row, hdict["Basis Verbrauch"]]);
            a.BasisLeistung = Helpers.GetDouble(arr[row, hdict["Basis Leistung"]]);
            a.BasisBlind = Helpers.GetDouble(arr[row, hdict["Basis Blind"]]);
            a.Betrag = Helpers.GetDouble(arr[row, hdict["Betrag"]]);
            a.MwStBetrag = Helpers.GetDouble(arr[row, hdict["MwSt-Betrag"]]);
            a.RechposBetraginklMwSt = Helpers.GetDouble(arr[row, hdict["Rechpos Betrag inkl. MwSt"]]);
            a.Verrechnungstyp = Helpers.GetString(arr[row, hdict["Verrechnungstyp"]]);
            a.VerrechnungstypArt = Helpers.GetString(arr[row, hdict["Verrechnungstyp Art"]]);
            a.VerrechnungstypKategorie = Helpers.GetString(arr[row, hdict["Verrechnungstyp Kategorie"]]);
            a.VerrechnungstypMessart = Helpers.GetString(arr[row, hdict["Verrechnungstyp Messart"]]);
            a.Vertragsart = Helpers.GetString(arr[row, hdict["Vertragsart"]]);
            a.Gruppe = Helpers.GetString(arr[row, hdict["Gruppe"]]);
            a.Ruecklieferung = Helpers.GetString(arr[row, hdict["Ruecklieferung"]]);
            a.VerrechnungstypEinheit = Helpers.GetString(arr[row, hdict["Verrechnungstyp Einheit"]]);
            a.MwStSatz = Helpers.GetDouble(arr[row, hdict["MwSt Satz"]]);
            a.RechposTage = Helpers.GetInt(arr[row, hdict["Rechpos Tage"]]);
            a.Tarif = Helpers.GetString(arr[row, hdict["Tarif"]]);
            a.Fakturierungsvariante = Helpers.GetString(arr[row, hdict["Fakturierungsvariante"]]);
            a.VertragspartnerAdresse = Helpers.GetString(arr[row, hdict["Vertragspartner Adresse"]]);
            a.Objektstandort = Helpers.GetString(arr[row, hdict["Objektstandort"]]);
            a.Rechnungsart = Helpers.GetString(arr[row, hdict["Rechnungsart"]]);
            a.VertragId = Helpers.GetInt(arr[row, hdict["Vertrag-Id"]]);
            a.SubjektId = Helpers.GetInt(arr[row, hdict["Subjekt-Id"]]);
            a.SammelrechnungId = Helpers.GetInt(arr[row, hdict["Sammelrechnung-Id"]]);
            a.ObjektIdVertrag = Helpers.GetInt(arr[row, hdict["Objekt-Id-Vertrag"]]);
            a.Marktprodukt = Helpers.GetString(arr[row, hdict["Marktprodukt"]]);
            a.StandortID = Helpers.GetInt(arr[row, hdict["Standort-ID"]]);
            int? objektid = Helpers.GetInt(arr[row, hdict["Objekt-ID Gebäude"]]);
            if (objektid != null) {
                a.ObjektIDGebäude = objektid;
            }
            else {
                a.ObjektIDGebäude = negativeIsnCounter--;
            }
        }
    }
}