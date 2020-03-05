using System;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._00_Import {
    public class TemperatureProfileImport {
        [NotNull]
        public string Bezeichnung { get; set; }
        public int Jahr { get; set; }
        [NPoco.Ignore]
        [SQLite.Ignore]
        [CanBeNull]
        public JsonSerializableProfile Profile { get; set; }
        [NotNull]
        [UsedImplicitly]
        public string JsonProfile {
            get => JsonConvert.SerializeObject(Profile, Formatting.Indented);
            set => Profile = JsonConvert.DeserializeObject<JsonSerializableProfile>(value);
        }

        [Obsolete("for json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public TemperatureProfileImport()
        {
        }
        public int ID { get; set; }
        public TemperatureProfileImport([NotNull] string bezeichnung, int jahr,  [NotNull] JsonSerializableProfile profile)
        {
            Bezeichnung = bezeichnung;
            Jahr = jahr;
            Profile = profile;
        }
    }
}