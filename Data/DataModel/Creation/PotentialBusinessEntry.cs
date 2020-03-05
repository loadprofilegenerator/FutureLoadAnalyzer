using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BurgdorfStatistics.DataModel.Src;
using Common.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(PotentialBusinessEntry))]
    [Table(nameof(PotentialBusinessEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class PotentialBusinessEntry:IGuidProvider {

        public PotentialBusinessEntry([JetBrains.Annotations.NotNull] string houseGuid,
                                      [JetBrains.Annotations.NotNull] string businessGuid,
                                      [JetBrains.Annotations.NotNull] string businessName,
                                      [JetBrains.Annotations.NotNull] string complexName,
                                      double lowVoltageYearlyElectricityUse,
                                      double highVoltageYearlyElectricityUse,
                                      double lowVoltageYearlyElectricityUseDaytime,
                                      double lowVoltageYearlyElectricityUseNighttime,
                                      double yearlyGasUse,
                                      double yearlyFernwärmeUse,
                                      double summerbaseGasUse,
                                      int employees,
                                      int numberOfLocalnetEntries,
                                      [JetBrains.Annotations.NotNull] string tarif,
                                      [CanBeNull] string myCategory,
                                      [JetBrains.Annotations.NotNull] string hausAnschlussGuid)
        {
            HouseGuid = houseGuid;
            Guid = businessGuid;
            BusinessName = businessName;
            ComplexName = complexName;
            LowVoltageYearlyElectricityUse = lowVoltageYearlyElectricityUse;
            HighVoltageYearlyElectricityUse = highVoltageYearlyElectricityUse;
            LowVoltageYearlyElectricityUseDaytime = lowVoltageYearlyElectricityUseDaytime;
            LowVoltageYearlyElectricityUseNighttime = lowVoltageYearlyElectricityUseNighttime;
            YearlyGasUse = yearlyGasUse;
            YearlyFernwärmeUse = yearlyFernwärmeUse;
            SummerbaseGasUse = summerbaseGasUse;
            Employees = employees;
            NumberOfLocalnetEntries = numberOfLocalnetEntries;
            Tarif = tarif;
            MyCategory = myCategory;
            HausAnschlussGuid = hausAnschlussGuid;
        }

        [Obsolete("for json only")]
        public PotentialBusinessEntry()
        {
        }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string BusinessName { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ComplexName { get; set; }

        public int Employees { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<Localnet> HighVoltageLocalnetEntries { get; set; } = new List<Localnet>();

        [JetBrains.Annotations.NotNull]
        public string HighVoltageLocalnetEntriesAsJson {
            get => JsonConvert.SerializeObject(HighVoltageLocalnetEntries, Formatting.Indented);
            set => HighVoltageLocalnetEntries = JsonConvert.DeserializeObject<List<Localnet>>(value);
        }

        public double HighVoltageYearlyElectricityUse { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string LocalnetLowVoltageEntriesAsJson {
            get => JsonConvert.SerializeObject(LowVoltageLocalnetEntries, Formatting.Indented);
            set => LowVoltageLocalnetEntries = JsonConvert.DeserializeObject<List<Localnet>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<Localnet> LowVoltageLocalnetEntries { get; set; } = new List<Localnet>();

        public double LowVoltageYearlyElectricityUse { get; set; }
        public double LowVoltageYearlyElectricityUseDaytime { get; set; }
        public double LowVoltageYearlyElectricityUseNighttime { get; set; }

        [CanBeNull]
        public string MyCategory { get; set; }

        public int NumberOfLocalnetEntries { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }

        public double SummerbaseGasUse { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Tarif { get; set; }

        public double YearlyFernwärmeUse { get; set; }
        public double YearlyGasUse { get; set; }
    }
}