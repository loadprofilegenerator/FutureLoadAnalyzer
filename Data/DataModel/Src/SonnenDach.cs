#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable IDE1006 // Naming Styles
using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

// ReSharper disable All

namespace BurgdorfStatistics.DataModel.Src {
    [TableName(nameof(SonnenDach))]
    [NPoco.PrimaryKey(nameof(ogc_fid))]
    [Table(nameof(SonnenDach))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    public class SonnenDach {
        public int ausrichtung { get; set; }

        public int bedarf_heizung { get; set; }

        public int bedarf_warmwasser { get; set; }

        public int df_nummer { get; set; }
        public long df_uid { get; set; }
        public int dg_heizung { get; set; }
        public int dg_waermebedarf { get; set; }
        public int duschgaenge { get; set; }
        public double flaeche { get; set; }
        public double flaeche_kollektoren { get; set; }
        public int gstrahlung { get; set; }
        public int gwr_egid { get; set; }
        public int klasse { get; set; }
        public int mstrahlung { get; set; }
        public int neigung { get; set; }
        public long objectid { get; set; }

        public int ogc_fid { get; set; }

        public int sb_objektart { get; set; }

        [JetBrains.Annotations.NotNull]
        public string sb_uuid { get; set; }

        public double shape_area { get; set; }
        public double shape_length { get; set; }
        public int stromertrag { get; set; }
        public int volumen_speicher { get; set; }
        public int waermeertrag { get; set; }
    }
}
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1707 // Identifiers should not contain underscores