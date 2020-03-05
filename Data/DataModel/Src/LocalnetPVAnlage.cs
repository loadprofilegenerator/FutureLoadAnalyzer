#pragma warning disable CA1707 // Identifiers should not contain underscores
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName("LocalnetPVAnlagen")]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table("LocalnetPVAnlagen")]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class LocalnetPVAnlage {
        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Bezeichnung { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Anlagenummer { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Adresse { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Hersteller { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Inbetriebnahme { get; set; }

        public double Leistungkwp { get; set; }
        public bool Eigenverbrauch { get; set; }
        public double HKoord { get; set; }
        public double VKoord { get; set; }

        [JetBrains.Annotations.NotNull]
        public string TranslatedAdress { get; set; }
    }
}
#pragma warning restore CA1707 // Identifiers should not contain underscores