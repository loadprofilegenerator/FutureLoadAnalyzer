using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(PotentialHeatingSystemEntry))]
    [NPoco.PrimaryKey(nameof(HeatingSystemID))]
    [Table(nameof(PotentialHeatingSystemEntry))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class PotentialHeatingSystemEntry {
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public PotentialHeatingSystemEntry()
        {
        }
        [CanBeNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        public List<int> OriginalIsns { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Geschäftspartner { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalIsns, Formatting.Indented);
            set => OriginalIsns = JsonConvert.DeserializeObject<List<int>>(value);
        }
        public PotentialHeatingSystemEntry([CanBeNull] string houseGuid, [CanBeNull] string heatingSystemGuid,
                                           [CanBeNull] List<int> originalIsns, [JetBrains.Annotations.NotNull] string geschäftspartner, [JetBrains.Annotations.NotNull] string standort)
        {
            HouseGuid = houseGuid;
            HeatingSystemGuid = heatingSystemGuid;
            OriginalIsns = originalIsns;
            Geschäftspartner = geschäftspartner;
            Standort = standort;
        }


        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int HeatingSystemID { get; set; }

        [CanBeNull]
        public string HouseGuid { get; set; }

        [CanBeNull]
        public string HeatingSystemGuid { get; set; }

        public HeatingSystemType HeatingSystemType { get; set; }

        public double YearlyEnergyDemand { get; set; }
        public int Age { get; set; }
        public double AverageHeatingEnergyDemandDensity { get; set; }
        public double YearlyGasDemand { get; set; }
        public double YearlyFernwärmeDemand { get; set; }
    }
}