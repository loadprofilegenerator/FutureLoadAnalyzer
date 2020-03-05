using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(IncomingCommuterEntry))]
    [Table(nameof(IncomingCommuterEntry))]
    [NPoco.PrimaryKey(nameof(CommuterID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class IncomingCommuterEntry {
        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int CommuterID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string CommuterGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string TargetHouseGuid { get; set; }

        public double DistanceInKm { get; set; }
        public CommuntingMethod CommuntingMethod { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Wohngemeinde { get; set; }

        [JetBrains.Annotations.NotNull]
        public string WohnKanton { get; set; }

        [JetBrains.Annotations.NotNull]
        public string BusinessGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }
    }
}