using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Data.DataModel.Profiles;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Export {
    [TableName(nameof(HouseProfile))]
    [Table(nameof(HouseProfile))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class HouseProfile {
        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }
        [JetBrains.Annotations.NotNull]
        public string HouseName { get; set; }

        [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
        public double ValueCount {
            get => Profile.Values.Count;
            set { }
        }

        [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
        public double Max {
            get => Profile.Values.Max();
            set { }
        }

        [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
        public double Sum {
            get => Profile.EnergySum();
            set { }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public Profile Profile { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ProfileAsJson {
            get => JsonConvert.SerializeObject(Profile, Formatting.Indented);
            set => Profile = JsonConvert.DeserializeObject<Profile>(value);
        }
    }
}