using System.Diagnostics.CodeAnalysis;
using Common.Database;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(StreetLightingEntry))]
    [Table(nameof(StreetLightingEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class StreetLightingEntry :IGuidProvider {
        [SQLite.PrimaryKey]
        [AutoIncrement]

        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        public double YearlyElectricityUse { get; set; }
    }
}