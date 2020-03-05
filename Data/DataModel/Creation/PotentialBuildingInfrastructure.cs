using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(PotentialBuildingInfrastructure))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(PotentialBuildingInfrastructure))]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class PotentialBuildingInfrastructure:IGuidProvider {
        public PotentialBuildingInfrastructure([CanBeNull] string houseGuid,
                                               [JetBrains.Annotations.NotNull] string geschäftspartner,
                                               double lowVoltageTotalElectricityDemand,
                                               double highVoltageTotalElectricityDemand,
                                               [JetBrains.Annotations.NotNull] [ItemNotNull]
                                               List<Localnet> localnetEntriesLowVoltage,
                                               [JetBrains.Annotations.NotNull] [ItemNotNull]
                                               List<Localnet> localnetEntriesHighVoltage,
                                               [JetBrains.Annotations.NotNull] string standort,[JetBrains.Annotations.NotNull] string buildingInfrastructureGuid)
        {
            HouseGuid = houseGuid;
            Geschäftspartner = geschäftspartner;
            LowVoltageTotalElectricityDemand = lowVoltageTotalElectricityDemand;
            if (highVoltageTotalElectricityDemand > 0) {
                throw new FlaException("Building infrastructure with high voltage is nonsense: " + geschäftspartner + " standort: " + standort);
            }

            HighVoltageTotalElectricityDemand = highVoltageTotalElectricityDemand;
            LocalnetEntriesLowVoltage = localnetEntriesLowVoltage;
            LocalnetEntriesHighVoltage = localnetEntriesHighVoltage;
            Standort = standort;
            var allEntries = localnetEntriesHighVoltage.ToList();
            allEntries.AddRange(localnetEntriesLowVoltage);
            Isns = allEntries.Select(x => x.ObjektIDGebäude ?? -1).Distinct().ToList();
            Guid = buildingInfrastructureGuid;

        }

        [Obsolete("Json only")]
        public PotentialBuildingInfrastructure()
        {
        }

        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]

        public string Geschäftspartner { get; set; }

        public double HighVoltageTotalElectricityDemand { get; set; }

        [CanBeNull]
        public string HouseGuid { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> Isns { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(Isns, Formatting.Indented);
            set => Isns = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [ItemNotNull]
        [JetBrains.Annotations.NotNull]
        public List<Localnet> LocalnetEntriesHighVoltage { get; set; }

        [JetBrains.Annotations.NotNull]
        public string LocalnetEntriesHighVoltageAsJson {
            get => JsonConvert.SerializeObject(LocalnetEntriesHighVoltage, Formatting.Indented);
            set => LocalnetEntriesHighVoltage = JsonConvert.DeserializeObject<List<Localnet>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<Localnet> LocalnetEntriesLowVoltage { get; set; }

        [JetBrains.Annotations.NotNull]
        public string LocalnetEntriesLowVoltageAsJson {
            get => JsonConvert.SerializeObject(LocalnetEntriesLowVoltage, Formatting.Indented);
            set => LocalnetEntriesLowVoltage = JsonConvert.DeserializeObject<List<Localnet>>(value);
        }

        public double LowVoltageTotalElectricityDemand { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }
    }
}