using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Common;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(BuildingInfrastructure))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(BuildingInfrastructure))]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class BuildingInfrastructure : ReduceableHouseComponent, IHouseComponent {
        [Obsolete("only json")]
        public BuildingInfrastructure()
        {
        }

        public BuildingInfrastructure([JetBrains.Annotations.NotNull] string name,
                                      double lowVoltageYearlyTotalElectricityUse,
                                      double highVoltageYearlyTotalElectricityUse,
                                      [JetBrains.Annotations.NotNull] List<int> originalISNs,
                                      int finalIsn,
                                      [CanBeNull] string houseGuid,
                                      [JetBrains.Annotations.NotNull] string hausAnschlussGuid,
                                      [JetBrains.Annotations.NotNull] string biGuid,
                                      [JetBrains.Annotations.NotNull] string standort,
                                      [JetBrains.Annotations.NotNull] string geschäftspartner)
        {
            Name = name;
            LocalnetLowVoltageYearlyTotalElectricityUse = lowVoltageYearlyTotalElectricityUse;
            if (highVoltageYearlyTotalElectricityUse > 0) {
                throw new FlaException("High voltage building infrastructure doesn't make sense: " + name);
            }

            LocalnetHighVoltageYearlyTotalElectricityUse = highVoltageYearlyTotalElectricityUse;
            OriginalISNs = originalISNs;
            FinalIsn = finalIsn;
            HouseGuid = houseGuid ?? throw new FlaException("No house guid was set");
            HausAnschlussGuid = hausAnschlussGuid;
            SourceGuid = biGuid;
            Guid = biGuid;
            Standort = standort;
            Geschäftspartner = geschäftspartner;
            EnergyType = EnergyType.Electricity;
        }

        public EnergyType EnergyType { get; set; } = EnergyType.Electricity;
        public int FinalIsn { get; set; }

        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        [JetBrains.Annotations.NotNull]
        public string Geschäftspartner { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public HouseComponentType HouseComponentType { get; } = HouseComponentType.Infrastructure;

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        public string Name { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public List<int> OriginalISNs { get; set; }

        public string SourceGuid { get; set; }
        public string Standort { get; set; }
    }
}