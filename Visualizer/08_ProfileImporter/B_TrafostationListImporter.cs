using System;
using System.Collections.Generic;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class B_TrafostationListImporter : RunableWithBenchmark {
        public B_TrafostationListImporter([NotNull] ServiceRepository services)
            : base(nameof(B_TrafostationListImporter), Stage.ProfileImport, 200, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            var arr = ExcelHelper.ExtractDataFromExcel(@"U:\SimZukunft\RawDataForMerging\2018.04.03_Trafodaten.xlsx", 1, "A1", "V170");

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1];
                if (o == null) {
                    throw new Exception("Value was null");
                }

                hdict.Add(o.ToString(), i + 1);
            }

            SqlConnection.RecreateTable<LocalnetTrafostation>(Stage.ProfileImport, Constants.PresentSlice);

            var db = SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;

            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                var bezeichnung = Helpers.GetStringNotNull(arr[row, hdict["Bezeichnung"]]);
                var seriennummer = Helpers.GetStringNotNull(arr[row, hdict["Seriennummer"]]);
                var hersteller = Helpers.GetStringNotNull(arr[row, hdict["Hersteller"]]);
                var art = Helpers.GetStringNotNull(arr[row, hdict["Art"]]);
                var status = Helpers.GetStringNotNull(arr[row, hdict["Status"]]);
                var eingebautInLagerort = Helpers.GetStringNotNull(arr[row, hdict["Eingebaut in/Lagerort"]]);
                var einbauort = Helpers.GetStringNotNull(arr[row, hdict["Einbauort"]]);
                var adresse = Helpers.GetStringNotNull(arr[row, hdict["Adresse"]]);
                var vorlage = Helpers.GetStringNotNull(arr[row, hdict["Vorlage"]]);
                var komponentenart = Helpers.GetStringNotNull(arr[row, hdict["Komponentenart"]]);
                var leistungkVa = Helpers.GetStringNotNull(arr[row, hdict["Leistung [kVA]"]]);
                var primärnennstromA = Helpers.GetStringNotNull(arr[row, hdict["Primärnennstrom [A]"]]);
                var sekundärnennstromA = Helpers.GetStringNotNull(arr[row, hdict["Sekundärnennstrom [A]"]]);
                var baujahr = Helpers.GetStringNotNull(arr[row, hdict["Baujahr"]]);
                var sekundärstromA = Helpers.GetStringNotNull(arr[row, hdict["Sekundärstrom (gemessen) [A]"]]);
                var schaltgruppe = Helpers.GetStringNotNull(arr[row, hdict["Schaltgruppe"]]);
                var kurzschlussspannung = Helpers.GetStringNotNull(arr[row, hdict["Kurzschlussspannung [%]"]]);
                var eisenverlusteW = Helpers.GetStringNotNull(arr[row, hdict["Eisenverluste [W]"]]);
                var kupferverlusteW = Helpers.GetStringNotNull(arr[row, hdict["Kupferverluste [W]"]]);
                var iksekkA = Helpers.GetStringNotNull(arr[row, hdict["Ik sek [kA]"]]);
                var betriebsstatus = Helpers.GetStringNotNull(arr[row, hdict["Betriebsstatus"]]);
                var a = new LocalnetTrafostation(bezeichnung, seriennummer,hersteller,art,status,eingebautInLagerort,
                    einbauort,adresse,vorlage,komponentenart,leistungkVa,primärnennstromA,
                    sekundärnennstromA,baujahr, sekundärstromA, schaltgruppe,kurzschlussspannung,
                    eisenverlusteW, kupferverlusteW, iksekkA, betriebsstatus);
                if (arr[row, hdict["Bezeichnung"]] == null) {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(arr[row, hdict["Bezeichnung"]].ToString())) {
                    continue;
                }

                db.Save(a);
            }

            db.CompleteTransaction();
        }
    }
}