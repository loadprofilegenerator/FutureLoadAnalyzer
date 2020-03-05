using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Dst {
    [TableName(nameof(MonthlyElectricityUsePerStandort))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(MonthlyElectricityUsePerStandort))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class MonthlyElectricityUsePerStandort {
        [CanBeNull]
        public string CleanedStandort { get; set; }

        [JsonIgnore]
        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public Dictionary<string, Dictionary<int, double>> ElectricityLocalNetMonthlyValuesByTarif { get; set; } =
            new Dictionary<string, Dictionary<int, double>>();

        [CanBeNull]
        public string ElectricityLocalnetMonthlyValuesByTarifAsJson {
            get => JsonConvert.SerializeObject(ElectricityLocalNetMonthlyValuesByTarif, Formatting.Indented);

            set => ElectricityLocalNetMonthlyValuesByTarif = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, double>>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JsonIgnore]
        [JetBrains.Annotations.NotNull]
        public Dictionary<string, Dictionary<int, double>> ElectricityNetzMonthlyValuesByTarif { get; set; } =
            new Dictionary<string, Dictionary<int, double>>();

        [CanBeNull]
        public string ElectricityNetzMonthlyValuesByTarifAsJson {
            get => JsonConvert.SerializeObject(ElectricityNetzMonthlyValuesByTarif, Formatting.Indented);

            set => ElectricityNetzMonthlyValuesByTarif = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, double>>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JsonIgnore]
        [JetBrains.Annotations.NotNull]
        public Dictionary<string, Dictionary<int, double>> ElectricityNetzübergabeMonthlyValuesByTarif { get; set; } =
            new Dictionary<string, Dictionary<int, double>>();

        [JsonIgnore]
        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public Dictionary<string, Dictionary<int, double>> FernwaermeMonthlyValuesByTarif { get; set; } =
            new Dictionary<string, Dictionary<int, double>>();

        [JetBrains.Annotations.NotNull]
        public string FernwaermeMonthlyValuesByTarifAsJson {
            get => JsonConvert.SerializeObject(FernwaermeMonthlyValuesByTarif, Formatting.Indented);

            set => FernwaermeMonthlyValuesByTarif = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, double>>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JsonIgnore]
        [JetBrains.Annotations.NotNull]
        public Dictionary<string, Dictionary<int, double>> GasMonthlyValuesByTarif { get; set; } = new Dictionary<string, Dictionary<int, double>>();

        [JetBrains.Annotations.NotNull]
        public string GasMonthlyValuesByTarifAsJson {
            get => JsonConvert.SerializeObject(GasMonthlyValuesByTarif, Formatting.Indented);

            set => GasMonthlyValuesByTarif = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, double>>>(value);
        }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<Localnet> LocalnetEntries { get; set; } = new List<Localnet>();

        [JetBrains.Annotations.NotNull]
        public string LocalnetEntriesAsJson {
            get {
                LocalnetEntries.Sort((x, y) => string.Compare(x.Verrechnungstyp, y.Verrechnungstyp, StringComparison.Ordinal));
                return JsonConvert.SerializeObject(LocalnetEntries, Formatting.Indented);
            }

            set {
                LocalnetEntries = JsonConvert.DeserializeObject<List<Localnet>>(value);
                LocalnetEntries.Sort((x, y) => string.Compare(x.Verrechnungstyp, y.Verrechnungstyp, StringComparison.Ordinal));
            }
        }

        public int NumberOfEntries { get; set; }

        [CanBeNull]
        public string Standort { get; set; }

        public double YearlyElectricityUseLocalnet { get; set; }
        public double YearlyElectricityUseNetz { get; set; }
        public double YearlyElectricityUseNetzübergabe { get; set; }
        public double YearlyFernwaermeUse { get; set; }
        public double YearlyGasUse { get; set; }

        public void AddLocalnetEntryForStandort([JetBrains.Annotations.NotNull] Localnet localnet, Verbrauchsart vba)
        {
            if (!localnet.BasisVerbrauch.HasValue) {
                return;
            }

            IntegrityCheck();
            LocalnetEntries.Add(localnet);
            NumberOfEntries++;
            var verbrauch = localnet.BasisVerbrauch.Value;
            if (localnet.TerminString == null) {
                throw new Exception("was null");
            }

            var month = Convert.ToInt32(localnet.TerminString.Substring(0, 2), CultureInfo.InvariantCulture);
            if (localnet.Fakturierungsvariante == "Monat") {
                if (localnet.Verrechnungstyp == null) {
                    throw new Exception("was null");
                }

                WriteToDict(localnet.Fakturierungsvariante, month, verbrauch, localnet.Verrechnungstyp, vba);
                return;
            }

            if (localnet.TerminQuartal == null) {
                throw new Exception("was null");
            }

            var quartal = GetQuartal(localnet.TerminQuartal);
            var quartalfakturierungen = new List<string> {
                "Quartal (P 41/44/47/50)",
                "Quartal (P 43/46/49/52)",
                "Quartal (P 42/45/48/51)"
            };
            if (quartalfakturierungen.Contains(localnet.Fakturierungsvariante)) {
                if (localnet.Fakturierungsvariante == null) {
                    throw new Exception("was null");
                }

                if (localnet.Verrechnungstyp == null) {
                    throw new Exception("was null");
                }

                AddForRightMonths(quartal, verbrauch, localnet.Fakturierungsvariante, localnet.Verrechnungstyp, vba);
                IntegrityCheck();
                return;
            }

            throw new Exception("Unknown faktorierung: " + localnet.Fakturierungsvariante);
        }

        public void IntegrityCheck()
        {
            double electricitySum = 0;
            foreach (var monthlyvalues in ElectricityLocalNetMonthlyValuesByTarif.Values) {
                electricitySum += monthlyvalues.Values.Sum();
            }

            if (Math.Abs(YearlyElectricityUseLocalnet - electricitySum) > 0.1) {
                throw new Exception("Invalid electricity sum");
            }
        }

        [JetBrains.Annotations.NotNull]
        public static string MakeMonthlyTarifKey([JetBrains.Annotations.NotNull] string tarif,
                                                 [JetBrains.Annotations.NotNull] string verrechnungstyp) =>
            tarif + "#" + verrechnungstyp;

        public void WriteToDict([JetBrains.Annotations.NotNull] string tarif,
                                int month,
                                double value,
                                [JetBrains.Annotations.NotNull] string verrechnungstyp,
                                Verbrauchsart vba)
        {
            var key = MakeMonthlyTarifKey(tarif, verrechnungstyp);
            Dictionary<string, Dictionary<int, double>> dict;
            if (vba == Verbrauchsart.ElectricityLocalnet) {
                dict = ElectricityLocalNetMonthlyValuesByTarif;
                YearlyElectricityUseLocalnet += value;
            }
            else if (vba == Verbrauchsart.Gas) {
                dict = GasMonthlyValuesByTarif;
                YearlyGasUse += value;
            }
            else if (vba == Verbrauchsart.Fernwaerme) {
                dict = FernwaermeMonthlyValuesByTarif;
                YearlyFernwaermeUse += value;
            }
            else if (vba == Verbrauchsart.ElectricityNetz) {
                dict = ElectricityNetzMonthlyValuesByTarif;
                YearlyElectricityUseNetz += value;
            }
            else {
                throw new Exception("Unknown verbrauchsart");
            }

            if (!dict.ContainsKey(key)) {
                dict.Add(key, new Dictionary<int, double>());
            }

            if (!dict[key].ContainsKey(month)) {
                dict[key].Add(month, 0);
            }

            dict[key][month] += value;
        }

        private void AddForRightMonths(int quartal,
                                       double verbrauch,
                                       [JetBrains.Annotations.NotNull] string tarif,
                                       [JetBrains.Annotations.NotNull] string verrechnungstyp,
                                       Verbrauchsart vba)
        {
            int[] months;
            switch (quartal) {
                case 1:
                    months = new[] {1, 2, 3};
                    break;
                case 2:
                    months = new[] {4, 5, 6};
                    break;
                case 3:
                    months = new[] {7, 8, 9};
                    break;
                case 4:
                    months = new[] {10, 11, 12};
                    break;
                default: throw new Exception("Unknown quartal");
            }

            var key = MakeMonthlyTarifKey(tarif, verrechnungstyp);
            Dictionary<string, Dictionary<int, double>> dict;
            switch (vba) {
                case Verbrauchsart.ElectricityLocalnet:
                    dict = ElectricityLocalNetMonthlyValuesByTarif;
                    YearlyElectricityUseLocalnet += verbrauch;
                    break;
                case Verbrauchsart.Gas:
                    dict = GasMonthlyValuesByTarif;
                    YearlyGasUse += verbrauch;
                    break;
                case Verbrauchsart.Fernwaerme:
                    dict = FernwaermeMonthlyValuesByTarif;
                    YearlyFernwaermeUse += verbrauch;
                    break;
                case Verbrauchsart.ElectricityNetz:
                    dict = ElectricityNetzMonthlyValuesByTarif;
                    YearlyElectricityUseNetz += verbrauch;
                    break;
                case Verbrauchsart.Netzuebergabe:
                    dict = ElectricityNetzübergabeMonthlyValuesByTarif;
                    YearlyElectricityUseNetzübergabe += verbrauch;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vba), vba, null);
            }

            if (!dict.ContainsKey(key)) {
                dict.Add(key, new Dictionary<int, double>());
                for (var i = 1; i < 13; i++) {
                    dict[key].Add(i, 0);
                }
            }

            foreach (var month in months) {
                dict[key][month] += verbrauch / months.Length;
            }
        }

        private static int GetQuartal([JetBrains.Annotations.NotNull] string localnetTerminQuartal)
        {
            switch (localnetTerminQuartal) {
                case "1. Quartal":
                    return 1;
                case "2. Quartal":
                    return 2;
                case "3. Quartal":
                    return 3;
                case "4. Quartal":
                    return 4;
                default:
                    throw new Exception("Unknown quartal: " + localnetTerminQuartal);
            }
        }
    }
}