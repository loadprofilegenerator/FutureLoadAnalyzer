using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(ParkingSpace))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(ParkingSpace))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class ParkingSpace : IHouseComponent {
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public ParkingSpace()
        {
        }

        public ParkingSpace([CanBeNull] string householdGuid, [JetBrains.Annotations.NotNull] string parkingSpaceGuid, [JetBrains.Annotations.NotNull] string houseGuid,
                            [JetBrains.Annotations.NotNull] string hausAnschlussGuid, [JetBrains.Annotations.NotNull] string name)
        {
            HouseholdGuid = householdGuid;
            Guid = parkingSpaceGuid;
            HouseGuid = houseGuid;
            HausAnschlussGuid = hausAnschlussGuid;
            Name = name;
        }

        [CanBeNull]
        public string CarGuid { get; set; }

        public ChargingStationType ChargingStationType { get; set; }

        public int HouseComponentTypeInt {
            get => (int)HouseComponentType;
            set => HouseComponentType = (HouseComponentType)value;
        }

        [CanBeNull]
        public string HouseholdGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        public int FinalIsn { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        public double LocalnetHighVoltageYearlyTotalElectricityUse { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public HouseComponentType HouseComponentType { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        public EnergyType EnergyType { get; set; } = EnergyType.Other;

        public double LocalnetLowVoltageYearlyTotalElectricityUse { get; set; }
        public string Name { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> OriginalISNs { get; set; } = new List<int>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        public string SourceGuid => Guid;

        [JetBrains.Annotations.CanBeNull]
        public string Standort { get; set; }

        public double EffectiveEnergyDemand { get; set; }
    }
}