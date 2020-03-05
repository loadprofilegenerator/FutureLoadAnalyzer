using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using JetBrains.Annotations;

namespace BurgdorfStatistics._03_KomplexEnergy {
    [SuppressMessage("ReSharper", "IdentifierWordIsNotInDictionary")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class C_MakeMonthlyElectrictyUse : RunableWithBenchmark {
        private double _stromNetzSum { get; set; }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<MonthlyElectricityUsePerStandort>(Stage.ComplexEnergyData, Constants.PresentSlice);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
            var localnetEntries = dbRaw.Fetch<Localnet>();
            localnetEntries.Sort(Comparison);
            var clds = new Dictionary<string, MonthlyElectricityUsePerStandort>();
            dbEnergy.BeginTransaction();
            foreach (var localnet in localnetEntries) {
                if (string.IsNullOrWhiteSpace(localnet.Objektstandort)) {
                    continue;
                }

                var cleanedStandort = Helpers.CleanAdressString(localnet.Objektstandort);
                if (!clds.ContainsKey(cleanedStandort)) {
                    var cld1 = new MonthlyElectricityUsePerStandort {
                        Standort = localnet.Objektstandort,
                        CleanedStandort = cleanedStandort
                    };
                    clds.Add(cleanedStandort, cld1);
                }

                var cld = clds[cleanedStandort];
                ProcessVerrechnungstyp(localnet, cld);
                var totalSumFromMonthly = clds.Values.Select(x => x.YearlyElectricityUseNetz).Sum();
                if (Math.Abs(totalSumFromMonthly - _stromNetzSum) > 0.00001) {
                    throw new Exception("totalSumFromMonthly:" + totalSumFromMonthly + " stromNetzSum:" + _stromNetzSum);
                }
            }

            foreach (var cld in clds.Values) {
                cld.IntegrityCheck();
                dbEnergy.Save(cld);
            }

            dbEnergy.CompleteTransaction();
        }

        private static int Comparison([NotNull] Localnet x, [NotNull] Localnet y)
        {
            if (x.Objektstandort != y.Objektstandort) {
                return string.Compare(x.Objektstandort, y.Objektstandort, StringComparison.Ordinal);
            }

            return string.Compare(x.TerminString, y.TerminString, StringComparison.Ordinal);
        }

        private void ProcessVerrechnungstyp([NotNull] Localnet localnet, [NotNull] MonthlyElectricityUsePerStandort meps)
        {
            var processed = false;
            switch (localnet.Verrechnungstyp) {
                case "Energie Tagesstrom (HT)":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "Energie Nachtstrom (NT)":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "Gutschrift Strom Energie":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "Nachbelastung Strom Energie":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "Rücklieferung Tagesstrom HT":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "Rücklieferung Nachtstrom NT":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "EVG Tagesstrom (HT)":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "EVG Nachtstrom (NT)":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityLocalnet);
                    processed = true;
                    break;
                case "Gutschrift Erdgas":
                    break;
                case "Kosten Erdgasbezug":
                    break;
                case "Erdgasverbrauch":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.Gas);
                    processed = true;
                    break;
                case "CO2-Abgabe":
                    break;
                case "Grundpreis Erdgas":
                    break;
                case "Grundgebühr Erdgas":
                    break;
                case "Leistungsspitze":
                    break;
                case "Arbeitspreis":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.Fernwaerme);
                    processed = true;
                    break;
                case "Grundpreis Wärme (kW/Mt)":
                    break;
                case "Grundpreis Wärme (Zähler/Mt)":
                    break;
                case "Grundpreis 1 (kW/Mt)":
                    break;
                case "Grundpreis 2 (kW/Mt)":
                    break;
                case "Leistung":
                    break;
                case "Blindenergie (HT)":
                    break;
                case "Blindenergie (NT)":
                    break;
                case "Grundpreis Strom":
                    break;
                case "Netz Tagesstrom (HT)":
                    if (localnet.BasisVerbrauch == null) {
                        throw new Exception("Basisverbrauch was null");
                    }

                    _stromNetzSum += localnet.BasisVerbrauch.Value;
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityNetz);
                    break;
                case "Netz Nachtstrom (NT)":
                    if (localnet.BasisVerbrauch == null) {
                        throw new Exception("Basisverbrauch was null");
                    }

                    _stromNetzSum += localnet.BasisVerbrauch.Value;
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityNetz);
                    break;
                case "Messung und Abrechnung":
                    break;
                case "Systemdienstleistungen Swissgrid (SDL)":
                    break;
                case "Kostendeckende Einspeisevergütung KEV":
                    break;
                case "Abgabe an Gemeinde":
                    break;
                case "Gutschrift Strom Netznutzung":
                    //meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityNetz);
                    break;
                case "Bundesabgabe zum Schutz der Gewässer und Fische":
                    break;
                case "Netz Rücklieferung Tagesstrom HT":
                    //meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityNetz);
                    break;
                case "Netz Rücklieferung Nachtstrom NT":
                    //meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.ElectricityNetz);
                    break;
                case "Messung und Abrechung Netzübergabestelle":
                    meps.AddLocalnetEntryForStandort(localnet, Verbrauchsart.Netzuebergabe);
                    break;
                case "EVG Leistung":
                    break;
                case "EVG Blindenergie (HT)":
                    break;
                case "EVG Blindenergie (NT)":
                    break;
                default:
                    throw new Exception("unknown verrechnungstyp");
            }

            if (!processed && localnet.BasisVerbrauch > 0 && localnet.Vertragsart != "Netz") {
                Log(MessageType.Info, "Verbrauch, but not processed for " + localnet.Verrechnungstyp);
            }
        }

        public C_MakeMonthlyElectrictyUse([NotNull] ServiceRepository services)
            : base(nameof(C_MakeMonthlyElectrictyUse), Stage.ComplexEnergyData, 3, services, true)
        {
        }
    }
}