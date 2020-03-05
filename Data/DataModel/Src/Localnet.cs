using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

// ReSharper disable All

namespace BurgdorfStatistics.DataModel.Src {
    [TableName(nameof(Localnet))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(Localnet))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    public class Localnet {
        [CanBeNull]
        public double? Basis { get; set; }

        [CanBeNull]
        public double? BasisBlind { get; set; }

        [CanBeNull]
        public double? BasisLeistung { get; set; }

        [CanBeNull]
        public double? BasisVerbrauch { get; set; }

        [CanBeNull]
        public double? Betrag { get; set; }

        [CanBeNull]
        public string Fakturierungsvariante { get; set; }

        [CanBeNull]
        public string Gruppe { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [CanBeNull]
        public string Marktprodukt { get; set; }

        [CanBeNull]
        public double? MwStBetrag { get; set; }

        [CanBeNull]
        public double? MwStSatz { get; set; }

        [CanBeNull]
        public int? ObjektIDGebäude { get; set; }

        [CanBeNull]
        public int? ObjektIdVertrag { get; set; }

        [CanBeNull]
        public string Objektstandort { get; set; }

        [CanBeNull]
        public string Rechnungsart { get; set; }

        [CanBeNull]
        public double? RechposBetraginklMwSt { get; set; }

        [CanBeNull]
        public int? RechposTage { get; set; }

        [CanBeNull]
        public string Ruecklieferung { get; set; }

        [CanBeNull]
        public int? SammelrechnungId { get; set; }

        [CanBeNull]
        public int? StandortID { get; set; }

        [CanBeNull]
        public int? SubjektId { get; set; }

        [CanBeNull]
        public string Tarif { get; set; }

        public long Termin { get; set; }

        [CanBeNull]
        public int? TerminJahr { get; set; }

        [CanBeNull]
        public string TerminQuartal { get; set; }

        [CanBeNull]
        public string TerminSemester { get; set; }

        [CanBeNull]
        public string TerminString { get; set; }

        [CanBeNull]
        public string Verrechnungstyp { get; set; }

        [CanBeNull]
        public string VerrechnungstypArt { get; set; }

        [CanBeNull]
        public string VerrechnungstypEinheit { get; set; }

        [CanBeNull]
        public string VerrechnungstypKategorie { get; set; }

        [CanBeNull]
        public string VerrechnungstypMessart { get; set; }

        [CanBeNull]
        public int? VertragId { get; set; }

        [CanBeNull]
        public string Vertragsart { get; set; }

        [CanBeNull]
        public string VertragspartnerAdresse { get; set; }

        public bool IsEnergyValue()
        {
            if (BasisVerbrauch == null) {
                return false;
            }

            if (BasisVerbrauch == 0)
                return false;
            if (VerrechnungstypArt == "Strom" && VerrechnungstypKategorie == "Energie")
                return false;
            return true;
        }
    }
}