using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(IncomingCommuter))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(IncomingCommuter))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class IncomingCommuter {
        [JetBrains.Annotations.NotNull]
        public string Wohngemeinde { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Wohnkanton { get; set; }

        public double Entfernung { get; set; }
        public int Erwerbstätige { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }
    }
}