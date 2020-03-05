using System;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Export {
    [TableName(nameof(ResidualProfile))]
    [Table(nameof(ResidualProfile))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class ResidualProfile {
        public ResidualProfile([JetBrains.Annotations.NotNull] string name) => Name = name;

        [Obsolete("json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public ResidualProfile()
        {
        }

        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        [UsedImplicitly]
        public string JsonProfile {
            get => JsonConvert.SerializeObject(Profile, Formatting.Indented);
            set => Profile = JsonConvert.DeserializeObject<JsonSerializableProfile>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [CanBeNull]
        public JsonSerializableProfile Profile { get; set; }
    }
}