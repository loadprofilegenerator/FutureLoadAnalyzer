using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;
using Common;
using Data.DataModel.Export;
using Newtonsoft.Json.Converters;

namespace Data.DataModel.Creation {
    [TableName(nameof(BusinessEntry))]
    [Table(nameof(BusinessEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class BusinessEntry : ReduceableHouseComponent, IHouseComponent {
        [Obsolete("json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public BusinessEntry()
        {
        }

        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public BusinessEntry([JetBrains.Annotations.NotNull] string guid,
                             [JetBrains.Annotations.NotNull] string businessName,
                             BusinessType businessType,
                             [JetBrains.Annotations.NotNull] string complexName)
        {
            Guid = guid;
            BusinessName = businessName;
            BusinessType = businessType;
            ComplexName = complexName;
            EnergyType = EnergyType.Electricity;
        }

        public BusinessEntry([JetBrains.Annotations.NotNull] PotentialBusinessEntry p, BusinessType businessType)
        {
            HouseGuid = p.HouseGuid;
            Guid = p.Guid;
            BusinessName = p.BusinessName;
            LocalnetLowVoltageYearlyTotalElectricityUse = p.LowVoltageYearlyElectricityUse;
            LowVoltageYearlyElectricityUseDaytime = p.LowVoltageYearlyElectricityUseDaytime;
            LowVoltageYearlyElectricityUseNighttime = p.LowVoltageYearlyElectricityUseNighttime;
            LocalnetHighVoltageYearlyTotalElectricityUse = p.HighVoltageYearlyElectricityUse;
            YearlyGasUse = p.YearlyGasUse;
            YearlyFernwärmeUse = p.YearlyFernwärmeUse;
            SummerbaseGasUse = p.SummerbaseGasUse;
            BusinessType = businessType;
            Standort = p.Standort;
            Employees = p.Employees;
            LocalnetEntriesAsJson = p.LocalnetLowVoltageEntriesAsJson;
            LocalnetEntries.AddRange(p.HighVoltageLocalnetEntries);
            NumberOfLocalnetEntries = p.NumberOfLocalnetEntries;
            Tarif = p.Tarif;
            MyCategory = p.MyCategory;
            ComplexName = p.ComplexName;
            if (LocalnetEntries.Count == 0) {
                throw new FlaException("No localnet entries were found for this business?" + BusinessName);
            }

            OriginalISNs = LocalnetEntries.Select(x => x.ObjektIDGebäude ?? -1).Distinct().ToList();
            if (LocalnetEntries.Any(x => x.Tarif == "MS")) {
                IsMittelSpannungsAnschluss = true;
            }
            else {
                IsMittelSpannungsAnschluss = false;
            }

            HausAnschlussGuid = p.HausAnschlussGuid;
            Name = p.BusinessName;
            HouseComponentType = HouseComponentType.BusinessNoLastgangLowVoltage;
            EnergyType = EnergyType.Electricity;
        }

        [JetBrains.Annotations.NotNull]
        public string BusinessName { get; set; }

        public BusinessType BusinessType { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ComplexName { get; set; }

        public int Employees { get; set; }

        public EnergyType EnergyType { get; set; }

        public int FinalIsn { get; set; }
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public HouseComponentType HouseComponentType { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        public bool IsMittelSpannungsAnschluss { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

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


        public double LowVoltageYearlyElectricityUseDaytime { get; set; }

        public double LowVoltageYearlyElectricityUseNighttime { get; set; }

        [CanBeNull]
        public string MyCategory { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }

        public int NumberOfLocalnetEntries { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> OriginalISNs { get; set; } = new List<int>();

        [CanBeNull]
        public string RlmProfileName { get; set; }

        [JetBrains.Annotations.NotNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        public string SourceGuid => Guid;

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }

        public double SummerbaseGasUse { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Tarif { get; set; }

        public double YearlyFernwärmeUse { get; set; }

        public double YearlyGasUse { get; set; }
    }
}