using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.ProfileImport {
    [TableName(nameof(BkwProfile))]
    [Table(nameof(BkwProfile))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class BkwProfile {
        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public JsonSerializableProfile Profile { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }
        public int ID { get; set; }

        public int ValueCount {
            get => Profile.Values.Count;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        [JetBrains.Annotations.NotNull]
        [UsedImplicitly]
        public string JsonProfile {
            get => JsonConvert.SerializeObject(Profile, Formatting.Indented);
            set => Profile = JsonConvert.DeserializeObject<JsonSerializableProfile>(value);
        }
    }
}