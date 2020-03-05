using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class LightingProfile {
        public LightingProfile([NotNull] string name, [NotNull] JsonSerializableProfile profile, [NotNull] string guid)
        {
            Name = name;
            Profile = profile;
            Guid = guid;
        }
        public int ID { get; set; }

        [NotNull]
        public string Guid { get; set; }

        [NotNull]
        public string Name { get; set; }
        [NPoco.Ignore]
        [SQLite.Ignore]
        [NotNull]
        public JsonSerializableProfile Profile { get; set; }
        [NotNull]
        public string ProfileAsJson {
            get => JsonConvert.SerializeObject(Profile, Formatting.Indented);
            set => Profile = JsonConvert.DeserializeObject<JsonSerializableProfile>(value);
        }
    }
}