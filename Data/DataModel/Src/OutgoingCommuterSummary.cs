using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(OutgoingCommuterSummary))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(OutgoingCommuterSummary))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class OutgoingCommuterSummary {
        [JetBrains.Annotations.NotNull]
        public string Arbeitsgemeinde { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Arbeitskanton { get; set; }

        public double Entfernung { get; set; }
        public int Erwerbstätige { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }
    }
}