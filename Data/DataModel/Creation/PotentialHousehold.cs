using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(PotentialHousehold))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(PotentialHousehold))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class PotentialHousehold:IGuidProvider {
        public PotentialHousehold([JetBrains.Annotations.NotNull] string houseGuid, [JetBrains.Annotations.NotNull] string householdGuid,
                                  double yearlyElectricityUse, [JetBrains.Annotations.NotNull] string tarif, int numberOfLocalnetEntries,
                                  [JetBrains.Annotations.NotNull] string businessPartnerName, [JetBrains.Annotations.NotNull] string householdKey, [JetBrains.Annotations.NotNull] string hausAnschlussGuid,
                                  [JetBrains.Annotations.NotNull] string standort)
        {
            HouseGuid = houseGuid;
            Guid = householdGuid;
            YearlyElectricityUse = yearlyElectricityUse;
            Tarif = tarif;
            NumberOfLocalnetEntries = numberOfLocalnetEntries;
            BusinessPartnerName = businessPartnerName;
            HouseholdKey = householdKey;
            HausAnschlussGuid = hausAnschlussGuid;
            Standort = standort ?? throw new FlaException("standort was null");
        }

        [Obsolete("only for json")]
        public PotentialHousehold()
        {
        }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [JsonIgnore]
        public List<int> MonthlyEnergyUseIDs { get; set; } = new List<int>();

        [JetBrains.Annotations.NotNull]
        public string MonthlyEnergyUseIDsAsJson {
            get => JsonConvert.SerializeObject(MonthlyEnergyUseIDs, Formatting.Indented);
            set => MonthlyEnergyUseIDs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }


        public double YearlyElectricityUse { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Tarif { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<Localnet> LocalnetEntries { get; set; } = new List<Localnet>();

        [JetBrains.Annotations.NotNull]
        public string LocalnetEntriesAsJson {
            get => JsonConvert.SerializeObject(LocalnetEntries, Formatting.Indented);
            set => LocalnetEntries = JsonConvert.DeserializeObject<List<Localnet>>(value);
        }

        public int NumberOfLocalnetEntries { get; set; }

        [JetBrains.Annotations.NotNull]
        public string BusinessPartnerName { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }
    }
}