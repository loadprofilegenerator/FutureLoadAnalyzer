using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using JetBrains.Annotations;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace BurgdorfStatistics._00_Import {
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    // ReSharper disable once InconsistentNaming
    public class B01_LocalnetImport : RunableWithBenchmark {
        public B01_LocalnetImport([NotNull] ServiceRepository services)
            : base(nameof(B01_LocalnetImport), Stage.Raw,101, services, true)
        {
        }

        protected override void RunChartMaking()
        {
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var localNet = dbRaw.Fetch<Localnet>();
            var sumPerVerrechnungstyp = new Dictionary<string, double>();
            var verrechnungstypen = localNet.Select(x => x.Verrechnungstyp).Distinct().ToList();
            var sumPerTarif = new Dictionary<string, double>();
            var tarife = localNet.Select(x => x.Tarif).Distinct().ToList();
            foreach (var s in verrechnungstypen)
            {
                sumPerVerrechnungstyp.Add(s, 0);
            }

            foreach (var s in tarife)
            {
                sumPerTarif.Add(s, 0);
            }

            foreach (var l in localNet)
            {
                //ignore all the entries that don't have a standort
                if (string.IsNullOrWhiteSpace(l.Objektstandort))
                {
                    continue;
                }

                if (l.BasisVerbrauch != null && l.Verrechnungstyp != null)
                {
                    sumPerVerrechnungstyp[l.Verrechnungstyp] += l.BasisVerbrauch.Value;
                }

                if (l.BasisVerbrauch != null && l.Tarif != null)
                {
                    sumPerTarif[l.Tarif] += l.BasisVerbrauch.Value;
                }
            }

            foreach (var pair in sumPerVerrechnungstyp)
            {
                Log(MessageType.Info, "Verrechnungstyp:" + pair.Key + ": " + pair.Value);
            }

            foreach (var pair in sumPerTarif)
            {
                Log(MessageType.Info, "Tarif:" + pair.Key + ": " + pair.Value);
            }

        }

        protected override void RunActualProcess()
        {
            // ReSharper disable once StringLiteralTypo
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\reduced.xlsb", 1, "A1", "AH476000");
            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1];
                if (o == null) {
                    throw new Exception("was null");
                }

                hdict.Add(o.ToString(), i + 1);
            }

            SqlConnection.RecreateTable<Localnet>(Stage.Raw, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                var a = new Localnet();
                if (arr[row, hdict["Termin"]] == null) {
                    continue;
                }

                AssignFields(a, arr, row, hdict);
                db.Save(a);
            }

            db.CompleteTransaction();
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void AssignFields([NotNull] Localnet a, [ItemNotNull] [NotNull] object[,] arr, int row, [NotNull] Dictionary<string, int> hdict)
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
            a.ObjektIDGebäude = Helpers.GetInt(arr[row, hdict["Objekt-ID Gebäude"]]);
        }
    }
}