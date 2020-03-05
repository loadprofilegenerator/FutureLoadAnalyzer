using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Dst {
    [TableName(nameof(ComplexBuildingData))]
    [NPoco.PrimaryKey(nameof(ComplexBuildingDataID))]
    [Table(nameof(ComplexBuildingData))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ComplexBuildingData {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public long calc_ehzww { get; set; }
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public long calc_whzww { get; set; }
#pragma warning restore CA1707 // Identifiers should not contain underscores
        public int AnzahlWohnungenBern { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ComplexBuildingDataID { get; set; }

        [CanBeNull]
        public string ComplexName { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> GebäudeTypen { get; set; } = new List<string>();

        [JetBrains.Annotations.NotNull]
        public string GebäudeTypenAsJson {
            get => JsonConvert.SerializeObject(GebäudeTypen);
            set => GebäudeTypen = JsonConvert.DeserializeObject<List<string>>(value);
        }

        public int NumberEnergieBernBuildings { get; set; }
        public double TotalArea { get; set; }
        public int NumberOfMergedEntries { get; set; }


        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> BuildingAges { get; set; } = new List<int>();

        [JetBrains.Annotations.NotNull]
        public string BuildingAgesAsJson {
            get => JsonConvert.SerializeObject(BuildingAges);
            set => BuildingAges = JsonConvert.DeserializeObject<List<int>>(value);
        }

        public double TotalEnergieBezugsfläche { get; set; }
    }
}