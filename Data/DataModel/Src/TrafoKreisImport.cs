#pragma warning disable CA1707 // Identifiers should not contain underscores
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(TrafoKreisImport))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(TrafoKreisImport))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class TrafoKreisImport {
        [CanBeNull]
        public string U_STRASSE1 { get; set; }

        [JetBrains.Annotations.NotNull]
        public string U_STR_NR_I { get; set; }

        [CanBeNull]
        public int? U_EGID_ISE { get; set; }

        [CanBeNull]
        public int? U_OBJ_ID_I { get; set; }

        [JetBrains.Annotations.NotNull]

        public string U_TRAFOKRE { get; set; }


        [JetBrains.Annotations.NotNull]
        public string DESCRIPTIO { get; set; }

        [CanBeNull]
        public string u_Nr_Dez_E { get; set; }

        public double HKOORD { get; set; }
        public double VKOORD { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }
    }
}
#pragma warning restore CA1707 // Identifiers should not contain underscores