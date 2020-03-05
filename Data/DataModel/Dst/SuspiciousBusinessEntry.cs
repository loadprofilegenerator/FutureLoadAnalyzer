using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Dst {
    [TableName(nameof(SuspiciousBusinessEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(SuspiciousBusinessEntry))]
    public class SuspiciousBusinessEntry {
        public SuspiciousBusinessEntry([CanBeNull] string name, double electricity, double gasUse, double wärmeUse, [JetBrains.Annotations.NotNull] string standort)
        {
            Name = name;
            Electricity = electricity;
            GasUse = gasUse;
            WärmeUse = wärmeUse;
            Standort = standort;
        }

        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public SuspiciousBusinessEntry()
        {
        }

        [CanBeNull]
        public string Name { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        public double Electricity { get; set; }
        public double GasUse { get; set; }
        public double WärmeUse { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }
    }
}