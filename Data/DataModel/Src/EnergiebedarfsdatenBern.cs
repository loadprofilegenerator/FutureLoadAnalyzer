#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable IDE1006 // Naming Styles
using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

// ReSharper disable All

namespace BurgdorfStatistics.DataModel.Src {
    [TableName(nameof(EnergiebedarfsdatenBern))]
    [NPoco.PrimaryKey(nameof(ogc_fid))]
    [Table(nameof(EnergiebedarfsdatenBern))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class EnergiebedarfsdatenBern {
        public long calc_ehz { get; set; }

        public long calc_ehzww { get; set; }
        public long calc_eww { get; set; }
        public long calc_whz { get; set; }
        public long calc_whzww { get; set; }
        public long calc_www { get; set; }

        [JetBrains.Annotations.NotNull]
        public string dmutdat { get; set; }

        public long egid { get; set; }
        public int ganzwhg { get; set; }
        public long garea { get; set; }
        public int gastw { get; set; }
        public int gbauj { get; set; }
        public int gbaup { get; set; }
        public long geak_ebf { get; set; }
        public long geak_eff_h { get; set; }
        public long geak_ganzw { get; set; }
        public int genhz { get; set; }
        public int genww { get; set; }
        public int gheiz { get; set; }
        public int gkat { get; set; }
        public long gkodx { get; set; }
        public long gkody { get; set; }
        public int gstat { get; set; }
        public int gwwv { get; set; }
        public int has_geak { get; set; }
        public long objectid { get; set; }
        public int ogc_fid { get; set; }
        public long upd_ebf { get; set; }
        public long upd_ganzwh { get; set; }

        [JetBrains.Annotations.NotNull]
        public string upd_gdenam { get; set; }

        public long upd_genhz { get; set; }
        public int upd_genww { get; set; }

        [JetBrains.Annotations.NotNull]
        public string upd_gtyp { get; set; }

        [JetBrains.Annotations.NotNull]
        public string upd_qhz { get; set; }

        [JetBrains.Annotations.NotNull]
        public string upd_qww { get; set; }


        public long upd_wfla { get; set; }

    }
}
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1707 // Identifiers should not contain underscores