using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(DHWHeaterEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(DHWHeaterEntry))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class DHWHeaterEntry : IHouseComponent {
        private DhwHeatingSystem _dhwHeatingSystemType;
        [CanBeNull] private string _hausAnschlussGuid;

        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public DHWHeaterEntry()
        {
        }

        public DHWHeaterEntry([JetBrains.Annotations.NotNull] string houseGuid,
                              [JetBrains.Annotations.NotNull] string dhwSystemGuid,
                              [JetBrains.Annotations.NotNull] string name)
        {
            HouseGuid = houseGuid;
            Guid = dhwSystemGuid;
            Name = name;
        }

        public DhwHeatingSystem DhwHeatingSystemType {
            get => _dhwHeatingSystemType;
            set {
                _dhwHeatingSystemType = value;
                if (_dhwHeatingSystemType == DhwHeatingSystem.Heatpump) {
                    EnergyType = EnergyType.Electricity;
                    return;
                }

                if (_dhwHeatingSystemType == DhwHeatingSystem.Electricity) {
                    EnergyType = EnergyType.Electricity;
                    return;
                }

                EnergyType = EnergyType.Other;
            }
        }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        public double TotalEnergy { get; set; }

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
        public string HausAnschlussGuid {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
            get => _hausAnschlussGuid ?? throw new ArgumentOutOfRangeException(nameof(HausAnschlussGuid));
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
            set => _hausAnschlussGuid = value;
        }

        public HouseComponentType HouseComponentType { get; set; } = HouseComponentType.Dhw;

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

        [JetBrains.Annotations.NotNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        public string SourceGuid => Guid;

        [CanBeNull]
        public string Standort { get; set; }
    }
}