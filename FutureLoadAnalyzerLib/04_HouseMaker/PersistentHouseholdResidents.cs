using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SQLite;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class PersistentHouseholdResidents {
        // ReSharper disable once NotNullMemberIsNotInitialized
        public PersistentHouseholdResidents()
        {
        }

        public PersistentHouseholdResidents([JetBrains.Annotations.NotNull] string householdKey) => HouseholdKey = householdKey;
        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }
        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }
        [NPoco.Ignore]
        [Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<PersistentOccupant> Occupants { get; set; } = new List<PersistentOccupant>();
        [JetBrains.Annotations.NotNull]
        public string OccupantssAsJson {
            get => JsonConvert.SerializeObject(Occupants, Formatting.Indented);
            set => Occupants = JsonConvert.DeserializeObject<List<PersistentOccupant>>(value);
        }
    }
}