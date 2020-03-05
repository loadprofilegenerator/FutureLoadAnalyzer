#pragma warning disable CA1707 // Identifiers should not contain underscores
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

// ReSharper disable All
namespace BurgdorfStatistics.DataModel.Src {
    [TableName("Raw_GwrAdresse")]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table("Raw_GwrAdresse")]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    public class GwrAdresse {
        [CanBeNull]
        public int? AmtlicherAdresscode_DADRC { get; set; }

        [CanBeNull]
        public int? AmtlicheStrassennummer_DSTRANR { get; set; }

        [CanBeNull]
        public string BauprojektIdLiefersystem_DBABID { get; set; }

        [CanBeNull]
        public int? BFSGemeindenummer_GGDENR { get; set; }

        [CanBeNull]
        public int? EidgEingangsidentifikator_EDID { get; set; }

        public int EidgGebaeudeidentifikator_EGID { get; set; }

        [CanBeNull]
        public int? EidgStrassenidentifikator_DSTRID { get; set; }

        [CanBeNull]
        public string EingangsIdLiefersystem_DBADID { get; set; }

        [CanBeNull]
        public string EingangsnummerGebaeude_DEINR { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public double? EKoordinate_DKODE { get; set; }

        [CanBeNull]
        public int? ErhebungsstelleBaustatistik_DESTNR { get; set; }

        [CanBeNull]
        public int? Gebaeudeeingangstatus_DSTAT { get; set; }

        [CanBeNull]
        public int? GebaeudeIdLiefersystem_DBAGID { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public double? NKoordinate_DKODN { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public int? Plausibilitaetsstatus_DPLAUS { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public int? PLZZusatzziffer_DPLZZ { get; set; }

        [CanBeNull]
        public int? Postleitzahl_DPLZ4 { get; set; }

        [CanBeNull]
        public string Strassenbezeichnung_DSTR { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public double? XKoordinate_DKODX { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public double? YKoordinate_DKODY { get; set; }
    }
}
#pragma warning restore CA1707 // Identifiers should not contain underscores