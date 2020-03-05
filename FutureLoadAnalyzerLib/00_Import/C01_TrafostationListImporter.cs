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
    public class C01_TrafostationListImporter : RunableWithBenchmark {
        public C01_TrafostationListImporter([NotNull] ServiceRepository services)
            : base(nameof(C01_TrafostationListImporter), Stage.Raw, 201, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            string fn = CombineForRaw("2018.04.03_Trafodaten.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "V170", out var _);

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) ; i++) {
                var o = arr[0, i ];
                if (o == null) {
                    continue;
                }
                hdict.Add(o.ToString(), i );
            }

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<LocalnetTrafostation>();
            db.BeginTransaction();
            for (var row = 1; row < arr.GetLength(0); row++) {
                var bezeichnung = Helpers.GetString(arr[row, hdict["Bezeichnung"]]);
                var seriennummer = Helpers.GetString(arr[row, hdict["Seriennummer"]]);
                var hersteller = Helpers.GetString(arr[row, hdict["Hersteller"]]);
                var art = Helpers.GetString(arr[row, hdict["Art"]]);
                var status = Helpers.GetString(arr[row, hdict["Status"]]);
                var eingebautInLagerort = Helpers.GetString(arr[row, hdict["Eingebaut in/Lagerort"]]);
                var einbauort = Helpers.GetString(arr[row, hdict["Einbauort"]]);
                var adresse = Helpers.GetString(arr[row, hdict["Adresse"]]);
                var vorlage = Helpers.GetString(arr[row, hdict["Vorlage"]]);
                var komponentenart = Helpers.GetString(arr[row, hdict["Komponentenart"]]);
                var leistungkVa = Helpers.GetString(arr[row, hdict["Leistung [kVA]"]]);
                var primärnennstromA = Helpers.GetString(arr[row, hdict["Primärnennstrom [A]"]]);
                var sekundärnennstromA = Helpers.GetString(arr[row, hdict["Sekundärnennstrom [A]"]]);
                var baujahr = Helpers.GetString(arr[row, hdict["Baujahr"]]);
                var sekundärstromA = Helpers.GetString(arr[row, hdict["Sekundärstrom (gemessen) [A]"]]);
                var schaltgruppe = Helpers.GetString(arr[row, hdict["Schaltgruppe"]]);
                var kurzschlussspannung = Helpers.GetString(arr[row, hdict["Kurzschlussspannung [%]"]]);
                var eisenverlusteW = Helpers.GetString(arr[row, hdict["Eisenverluste [W]"]]);
                var kupferverlusteW = Helpers.GetString(arr[row, hdict["Kupferverluste [W]"]]);
                var iksekkA = Helpers.GetString(arr[row, hdict["Ik sek [kA]"]]);
                var betriebsstatus = Helpers.GetString(arr[row, hdict["Betriebsstatus"]]);
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