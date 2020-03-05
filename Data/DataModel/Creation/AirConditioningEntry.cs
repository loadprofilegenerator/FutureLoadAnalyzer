using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Export;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(AirConditioningEntry))]
    [Table(nameof(AirConditioningEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class AirConditioningEntry : IHouseComponent {
        // ReSharper disable once NotNullMemberIsNotInitialized
        [Obsolete("for json only")]
        public AirConditioningEntry()
        {
        }

        public AirConditioningEntry([JetBrains.Annotations.NotNull] string houseGuid,
                                    [JetBrains.Annotations.NotNull] string acGuid,
                                    double yearlyElectricityUse,
                                    double cop,
                                    AirConditioningType airConditioningType,
                                    [JetBrains.Annotations.NotNull] string hausAnschlussGuid,
                                    [JetBrains.Annotations.NotNull] string name,
                                    [JetBrains.Annotations.NotNull] string standort)
        {
            HouseGuid = houseGuid;
            Guid = acGuid;
            COP = cop;
            EffectiveEnergyDemand = yearlyElectricityUse;
            AirConditioningType = airConditioningType;
            HausAnschlussGuid = hausAnschlussGuid;
            Name = name;
            Standort = standort;
            EnergyType = EnergyType.Electricity;
        }

        public AirConditioningType AirConditioningType { get; set; }

        public double COP { get; set; }

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

        public double EffectiveEnergyDemand { get; set; }
        public EnergyType EnergyType { get; set; }

        public int FinalIsn { get; set; }

        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        public HouseComponentType HouseComponentType { get; set; } = HouseComponentType.Cooling;

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        public double LocalnetHighVoltageYearlyTotalElectricityUse { get; set; }
        public double LocalnetLowVoltageYearlyTotalElectricityUse { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> OriginalISNs { get; set; } = new List<int>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        public string SourceGuid => Guid;

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }
    }
}