using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

namespace Data.DataModel.Dst {
    [TableName(nameof(BuildingComplexStandorte))]
    [NPoco.PrimaryKey(nameof(ComplexEgidID))]
    [Table(nameof(BuildingComplexStandorte))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class BuildingComplexStandorte {
        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ComplexEgidID { get; set; }

        public int ComplexID { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public string ComplexName { get; set; }

        [JetBrains.Annotations.CanBeNull]
        public string Standort { get; set; }
    }
}