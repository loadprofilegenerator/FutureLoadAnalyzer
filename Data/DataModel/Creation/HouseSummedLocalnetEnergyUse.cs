using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(HouseSummedLocalnetEnergyUse))]
    [Table(nameof(HouseSummedLocalnetEnergyUse))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class HouseSummedLocalnetEnergyUse {
        [SQLite.PrimaryKey]
        [AutoIncrement]

        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        public double ElectricityUse { get; set; }
        public double ElectricityUseDayLow { get; set; }
        public double ElectricityUseDayHigh { get; set; }

        public double ElectricityUseNightLow { get; set; }
        public double ElectricityUseNightHigh { get; set; }
        public double GasUse { get; set; }
        public double WärmeUse { get; set; }
    }
}