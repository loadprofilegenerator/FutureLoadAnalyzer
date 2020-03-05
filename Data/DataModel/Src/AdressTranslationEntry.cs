using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(AdressTranslationEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(AdressTranslationEntry))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    public class AdressTranslationEntry {
        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        [CanBeNull]
        public string OriginalStandort { get; set; }

        [CanBeNull]
        public string TranslatedAdress { get; set; }
    }
}