using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.ProfileImport {
    [TableName(nameof(RlmProfile))]
    [Table(nameof(RlmProfile))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class RlmProfile {
        public RlmProfile([JetBrains.Annotations.NotNull] string name, int id, [JetBrains.Annotations.NotNull] JsonSerializableProfile profile)
        {
            Name = name;
            ID = id;
            Profile = profile;
        }

        [Obsolete("Json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public RlmProfile()
        {
        }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }
        public int ID { get; set; }

        public int ValueCount {
            get => Profile.Values.Count;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        public double SumElectricity {
            get => Profile.Values.Sum() / 4;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public JsonSerializableProfile Profile { get; set; }

        [JetBrains.Annotations.NotNull]
        [UsedImplicitly]
        public string JsonProfile {
            get => JsonConvert.SerializeObject(Profile, Formatting.Indented);
            set => Profile = JsonConvert.DeserializeObject<JsonSerializableProfile>(value);
        }
    }
}