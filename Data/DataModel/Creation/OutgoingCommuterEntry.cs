using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(OutgoingCommuterEntry))]
    [Table(nameof(OutgoingCommuterEntry))]
    [NPoco.PrimaryKey(nameof(CommuterID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class OutgoingCommuterEntry {
        public OutgoingCommuterEntry([JetBrains.Annotations.NotNull] string commuterGuid, [JetBrains.Annotations.NotNull] string householdGuid, double distanceInKm, CommuntingMethod communtingMethod, [JetBrains.Annotations.NotNull] string workCity, [JetBrains.Annotations.NotNull] string workKanton, [JetBrains.Annotations.NotNull] string houseGuid)
        {
            CommuterGuid = commuterGuid;
            HouseholdGuid = householdGuid;
            DistanceInKm = distanceInKm;
            CommuntingMethod = communtingMethod;
            WorkCity = workCity;
            WorkKanton = workKanton;
            HouseGuid = houseGuid;
        }

        public OutgoingCommuterEntry()
        {
        }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int CommuterID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string CommuterGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdGuid { get; set; }

        public double DistanceInKm { get; set; }
        public CommuntingMethod CommuntingMethod { get; set; }

        [JetBrains.Annotations.NotNull]
        public string WorkCity { get; set; }

        [JetBrains.Annotations.NotNull]
        public string WorkKanton { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }
    }
}