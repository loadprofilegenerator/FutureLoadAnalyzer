#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable All

using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Src;
using NPoco;
using SQLite;

namespace BurgdorfStatistics.DataModel.Src {
    [TableName(nameof(RasterDatenEnergiebedarfKanton))]
    [NPoco.PrimaryKey(nameof(ogc_fid))]
    [Table(nameof(RasterDatenEnergiebedarfKanton))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    public class RasterDatenEnergiebedarfKanton {
        public long cen_x { get; set; }
        public long cen_y { get; set; }

        [LongName("Energiebezugsfläche, berechnet ")]
        public long ebf { get; set; }

        [LongName("Energiebedarf Heizen")]
        public long ehz { get; set; }

        [LongName("Energiebedarf Heizen anderer Energieträger")]
        public long ehz_a { get; set; }

        [LongName("Energiebedarf Heizen Elektrizität")]
        public long ehz_el { get; set; }

        [LongName("Energiebedarf Heizen Fernwärme")]
        public long ehz_fw { get; set; }

        [LongName("Energiebedarf Heizen Gas")]
        public long ehz_gz { get; set; }

        [LongName("Energiebedarf Heizen Holz")]
        public long ehz_ho { get; set; }

        [LongName("Energiebedarf Heizen Kohle")]
        public long ehz_ko { get; set; }

        [LongName("Energiebedarf Heizen Heizöl ")]
        public long ehz_ol { get; set; }

        [LongName("Energiebedarf Heizen Sonnenkollektor ")]

        public long ehz_so { get; set; }

        [LongName("Energiebedarf Heizen unbekannter Energieträger ")]

        public long ehz_u { get; set; }

        [LongName("Energiebedarf Heizen Wärmepumpe")]
        public long ehz_wp { get; set; }

        [LongName("Energiebedarf Heizen + Warmwasser")]
        public long ehzww { get; set; }

        [LongName("Energiebedarf Heizen Provisorische Unterkunft ")]
        public long ehzww_1010 { get; set; }

        [LongName("Energiebedarf Heizen Einfamilienhaus, ohne Nebennutzung")]
        public long ehzww_1021 { get; set; }

        [LongName("Energiebedarf Heizen Mehrfamilienhaus, ohne Nebennutzung")]
        public long ehzww_1025 { get; set; }

        [LongName("Energiebedarf Heizen Wohngebäude mit Nebennutzung")]
        public long ehzww_1030 { get; set; }

        [LongName("Energiebedarf Heizen Gebäude mit teilweiser Wohnnutzung ")]
        public long ehzww_1040 { get; set; }

        [LongName("Energiebedarf Heizen Gebäude ohne Wohnnutzung")]
        public long ehzww_1060 { get; set; }

        [LongName("Energiebedarf Heizen Sonderbau")]
        public long ehzww_1080 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr vor 1919")]

        public long ehzww_8011 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1919 bis 1945")]
        public long ehzww_8012 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1946 bis 1960")]
        public long ehzww_8013 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1961 bis 1970")]
        public long ehzww_8014 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1971 bis 1980")]
        public long ehzww_8015 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1981 bis 1985")]
        public long ehzww_8016 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1986 bis 1990")]
        public long ehzww_8017 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1991 bis 1995")]
        public long ehzww_8018 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 1996 bis 2000")]
        public long ehzww_8019 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 2001 bis 2005")]
        public long ehzww_8020 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 2006 bis 2010")]
        public long ehzww_8021 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr 2011 bis 2015")]
        public long ehzww_8022 { get; set; }

        [LongName("Energiebedarf Heizen Baujahr nach 2015")]
        public long ehzww_8023 { get; set; }

        [LongName("Energiebedarf Warmwasser ")]
        public long eww { get; set; }

        [LongName("Energiebedarf Warmwasser anderer Energieträger")]
        public long eww_a { get; set; }

        [LongName("Energiebedarf Warmwasser Elektrizität")]
        public long eww_el { get; set; }

        [LongName("Energiebedarf Warmwasser Fernwärme ")]
        public long eww_fw { get; set; }

        [LongName("Energiebedarf Warmwasser Gas")]
        public long eww_gz { get; set; }

        [LongName("Energiebedarf Warmwasser Holz")]
        public long eww_ho { get; set; }

        [LongName("Energiebedarf Warmwassser Kohle")]
        public long eww_ko { get; set; }

        [LongName("Energiebedarf Warmwasser Heizöl")]
        public long eww_ol { get; set; }

        [LongName("Energiebedarf Warmwasser Sonnenkollektor")]
        public long eww_so { get; set; }

        [LongName("Energiebedarf Warmwasser unbekannter Energieträger")]
        public long eww_u { get; set; }

        [LongName("Energiebedarf Warmwasser Wärmepumpe")]
        public long eww_wp { get; set; }

        public long gde { get; set; }

        [JetBrains.Annotations.NotNull]
        public string gde_name { get; set; }

        public long objectid { get; set; }
        public int ogc_fid { get; set; }

        [LongName("Wohnfläche GWR, ergänzt")]
        public long wfla { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Provisorische Unterkunft")]
        public long wg_1010 { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Einfamilienhaus, ohne Nebennutzung")]
        public long wg_1021 { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Mehrfamilienhaus, ohne Nebennutzun")]
        public long wg_1025 { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Wohngebäude mit Nebennutzung")]
        public long wg_1030 { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Gebäude mit teilweiser Wohnnutzung")]
        public long wg_1040 { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Gebäude ohne Wohnnutzung")]

        public long wg_1060 { get; set; }

        [LongName("Anzahl Gebäude mit Wohnung Sonderbau")]
        public long wg_1080 { get; set; }

        [LongName("Anzahl EFH und MFH Gebäude mit GEAK")]

        public long wg_geak { get; set; }


        [LongName("Anzahl Gebäude mit mindestens einer Wohnung")]
        public long wg_gwr { get; set; }

        [LongName("Wärmebedarf für Heizen")]
        public long whz { get; set; }

        [LongName("Wärmebedarf für das Heizen und Warmwasser ")]

        public long whzww { get; set; }

        [LongName("Wärmebedarf für Warmwasser")]
        public long www { get; set; }
    }

}
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE1006 // Naming Styles