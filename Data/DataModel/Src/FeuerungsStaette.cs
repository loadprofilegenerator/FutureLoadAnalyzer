using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(FeuerungsStaette))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(FeuerungsStaette))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class FeuerungsStaette {
        [CanBeNull]
        public int? AnlageNr { get; set; }

        [CanBeNull]
        public string AnlageStatus { get; set; }

        [CanBeNull]
        public string Brennstoff { get; set; }

        [CanBeNull]
        public int? EDID { get; set; }

        [CanBeNull]
        public int? EGID { get; set; }

        [CanBeNull]
        public string Energienutzung { get; set; }

        [CanBeNull]
        public string Gebäudeart { get; set; }

        [CanBeNull]
        public string Hausnummer { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        [CanBeNull]
        public int? KesselBaujahr { get; set; }

        [CanBeNull]
        public int? KesselLeistung { get; set; }

        [CanBeNull]
        public string Ort { get; set; }

        [CanBeNull]
        public int? PLZ { get; set; }

        [CanBeNull]
        public string Strasse { get; set; }

        [CanBeNull]
        public int? XKoordinate { get; set; }

        [CanBeNull]
        public int? YKoordinate { get; set; }
    }
}