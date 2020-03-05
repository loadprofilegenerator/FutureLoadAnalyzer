using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(PvSystemEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(PvSystemEntry))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class PvSystemEntry : IHouseComponent {
        [Obsolete("for json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public PvSystemEntry()
        {
        }

        public PvSystemEntry([JetBrains.Annotations.NotNull] string houseguid,
                             [JetBrains.Annotations.NotNull] string pvGuid,
                             [JetBrains.Annotations.NotNull] string hausAnschlussGuid,
                             [JetBrains.Annotations.NotNull] string name,
                             [JetBrains.Annotations.NotNull] string erzeugerID,
                             int buildYear)
        {
            HouseGuid = houseguid;
            HausAnschlussGuid = hausAnschlussGuid;
            Guid = pvGuid;
            Name = name;
            Standort = erzeugerID;
            BuildYear = buildYear;
        }

        public int BuildYear { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }


        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<PVSystemArea> PVAreas { get; set; } = new List<PVSystemArea>();


        [JetBrains.Annotations.NotNull]
        public string PVAreasAsJson {
            get => JsonConvert.SerializeObject(PVAreas, Formatting.Indented);
            set => PVAreas = JsonConvert.DeserializeObject<List<PVSystemArea>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        public double EffectiveEnergyDemand {
            get {
                if (PVAreas.Count == 0) {
                    return 0;
                }

                return PVAreas.Sum(x => x.Energy);
            }
            //for json
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        public EnergyType EnergyType { get; set; } = EnergyType.Electricity;

        public int FinalIsn { get; set; }
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Generation;

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public HouseComponentType HouseComponentType => HouseComponentType.Photovoltaik;

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

        [CanBeNull]
        public string Standort { get; set; }
    }
}