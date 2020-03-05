#pragma warning disable CA1707 // Identifiers should not contain underscores
using JetBrains.Annotations;
using NPoco;
using SQLite;

// ReSharper disable All
namespace BurgdorfStatistics.DataModel.Src {
    [TableName(nameof(GwrData))]
    [Table(nameof(GwrData))]
    [NPoco.PrimaryKey(nameof(ID))]
    public class GwrData {
        [CanBeNull]
        public int? Abbruchjahr_GABBJ { get; set; }

        [CanBeNull]
        public int? Abbruchmonat_GABBM { get; set; }

        [CanBeNull]
        public int? AmtlicheGebaeudenummer_GEBNR { get; set; }

        [CanBeNull]
        public int? AnzahlEingangsrecords_GADOM { get; set; }

        [CanBeNull]
        public int? AnzahlGebaeudeeingaenge_GANZDOM { get; set; }

        [CanBeNull]
        public int? AnzahlGeschosse_GASTW { get; set; }

        [CanBeNull]
        public int? AnzahlseparateWohnraeume_GAZZI { get; set; }

        [CanBeNull]
        public int? AnzahlWohnungen_GANZWHG { get; set; }

        [CanBeNull]
        public int? AnzahlWohnungsrecords_GAWHG { get; set; }

        [CanBeNull]
        public int? Baujahr_GBAUJ { get; set; }

        [CanBeNull]
        public string Baumonat_GBAUM { get; set; }

        [CanBeNull]
        public int? Bauperiode_GBAUP { get; set; }

        [CanBeNull]
        public string BauprojektIdLiefersystem_GBABID { get; set; }

        [CanBeNull]
        public int? BFSGemeindenummer_GDENR { get; set; }

        public long DatumderletztenAenderung_GMUTDAT { get; set; }
        public long DatumdesExports_GEXPDAT { get; set; }

        [CanBeNull]
        public int? EidgBauprojektidentifikator_EPROID { get; set; }

        [CanBeNull]
        public int? EidgGebaeudeidentifikator_EGID { get; set; }

        [CanBeNull]
        public string EidgGrundstuecksidentifikator_GEGRID { get; set; }

        [CanBeNull]
        public double? EKoordinate_GKODE { get; set; }

        [CanBeNull]
        public int? EnergietraegerderHeizung_GENHZ { get; set; }

        [CanBeNull]
        public int? EnergietraegerfuerWarmwasser_GENWW { get; set; }

        [CanBeNull]
        public int? ErhebungsstelleBaustatistik_GESTNR { get; set; }

        [CanBeNull]
        public int? Gebaeudeflaeche_GAREA { get; set; }

        [CanBeNull]
        public string GebaeudeIDLiefersystem_GBAGID { get; set; }

        [CanBeNull]
        public int? Gebaeudekategorie_GKAT { get; set; }

        [CanBeNull]
        public int? Gebaeudeklasse_GKLAS { get; set; }

        [CanBeNull]
        public int? Gebaeudestatus_GSTAT { get; set; }

        [CanBeNull]
        public int? Grundbuchkreisnummer_GGBKR { get; set; }

        [CanBeNull]
        public int? Heizungsart_GHEIZ { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [CanBeNull]
        public int? Koordinatenherkunft_GKSCE { get; set; }

        [CanBeNull]
        public int? Lokalcode1_GLOC1 { get; set; }

        [CanBeNull]
        public int? Lokalcode2_GLOC2 { get; set; }

        [CanBeNull]
        public int? Lokalcode3_GLOC3 { get; set; }

        [CanBeNull]
        public int? Lokalcode4_GLOC4 { get; set; }

        [CanBeNull]
        public string NamedesGebaeudes_GBEZ { get; set; }

        [CanBeNull]
        public double? NKoordinate_GKODN { get; set; }

        [CanBeNull]
        public int? Parzellennummer_GPARZ { get; set; }

        [CanBeNull]
        public int? PlausibilitaetsstatusderKoordinaten_GKPLAUS { get; set; }

        [CanBeNull]
        public int? PlausibilitaetsstatusGebaeude_GPLAUS { get; set; }

        [CanBeNull]
        public int? Renovationsjahr_GRENJ { get; set; }

        [CanBeNull]
        public int? Renovationsmonat_GRENM { get; set; }

        [CanBeNull]
        public int? Renovationsperiode_GRENP { get; set; }

        [CanBeNull]
        public int? StatusWohnungsbestandes_GWHGSTD { get; set; }

        [CanBeNull]
        public int? VerifikationWohnungsbestand_GWHGVER { get; set; }

        [CanBeNull]
        public int? Warmwasserversorgung_GWWV { get; set; }

        [CanBeNull]
        public double? XKoordinate_GKODX { get; set; }

        [CanBeNull]
        public double? YKoordinate_GKODY { get; set; }
    }
}
#pragma warning restore CA1707 // Identifiers should not contain underscores