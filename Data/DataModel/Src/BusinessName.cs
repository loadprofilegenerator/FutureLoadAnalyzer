using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName("BusinessNames")]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table("BusinessNames")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class BusinessName {
        [CanBeNull]
        public string Name { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Category { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        [CanBeNull]
        public string Standort { get; set; }
    }
}