using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    public enum HouseComponentType {
        Unknown,
        Household,
        Infrastructure,
        Photovoltaik,
        BusinessNoLastgangLowVoltage,
        Kwkw,
        BusinessWithLastgangLowVoltage,
        HouseLoad,
        HouseGeneration,
        LastgangGeneration,
        StreetLight,
        BusinessNoLastgangHighVoltage,
        BusinessWithLastgangHighVoltage,
        OutboundElectricCommuter,
        Heating,
        Cooling,
        Dhw
    }

    public enum EnergyType {
        Unknown,
        Electricity,
        Gas,
        Oil,
        Wood,
        Other
    }

    [TableName(nameof(Household))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(Household))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Household : ReduceableHouseComponent, IHouseComponent {
        [Obsolete("for json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public Household()
        {
        }

        public Household([JetBrains.Annotations.NotNull] string name,
                         [JetBrains.Annotations.NotNull] string houseGuid,
                         [JetBrains.Annotations.NotNull] string hausAnschlussGuid,
                         [JetBrains.Annotations.NotNull] string complexName,
                         [CanBeNull] string standort,
                         [JetBrains.Annotations.NotNull] string guid)
        {
            Name = name;
            HouseGuid = houseGuid;
            HausAnschlussGuid = hausAnschlussGuid;
            Guid = guid;
            HouseholdKey = MakeHouseholdKey(complexName, standort, name);
            Guid = guid;
            Standort = standort;
        }

        public Household([JetBrains.Annotations.NotNull] PotentialHousehold ph)
        {
            HouseGuid = ph.HouseGuid;
            Guid = ph.Guid;
            MonthlyEnergyUseIDsAsJson = ph.MonthlyEnergyUseIDsAsJson;
            if (ph.Standort == null) {
                throw new FlaException("standort was null");
            }

            Standort = ph.Standort;
            LocalnetLowVoltageYearlyTotalElectricityUse = ph.YearlyElectricityUse;
            Tarif = ph.Tarif;
            LocalnetEntriesAsJson = ph.LocalnetEntriesAsJson;
            NumberOfLocalnetEntries = ph.NumberOfLocalnetEntries;
            HouseholdKey = ph.HouseholdKey;
            BusinessPartnerName = ph.BusinessPartnerName;
            OriginalISNs = LocalnetEntries.Select(x => x.ObjektIDGebäude ?? -1).Distinct().ToList();
            HausAnschlussGuid = ph.HausAnschlussGuid;
            Name = ph.BusinessPartnerName;
            if (LocalnetEntries.Any(x => x.Tarif == "MS")) {
                throw new FlaException("Haushalt mit Mittelspannungsanschluss ist unsinn.");
            }
        }

        [CanBeNull]
        public string BusinessPartnerName { get; set; }

        public EnergyType EnergyType { get; set; } = EnergyType.Electricity;

        public int FinalIsn { get; set; }
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }

        public int HeuristicFamiliySize { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public HouseComponentType HouseComponentType { get; set; } = HouseComponentType.Household;

        public int HouseComponentTypeInt {
            get => (int)HouseComponentType;
            set => HouseComponentType = (HouseComponentType)value;
        }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

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

        public string Name { get; set; }

        public int NumberOfLocalnetEntries { get; set; }

        [ItemNotNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [JsonIgnore]
        public List<Occupant> Occupants { get; set; } = new List<Occupant>();

        [JetBrains.Annotations.NotNull]
        public string OccupantsAsJson {
            get => JsonConvert.SerializeObject(Occupants, Formatting.Indented);
            set => Occupants = JsonConvert.DeserializeObject<List<Occupant>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> OriginalISNs { get; set; } = new List<int>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        public string SourceGuid => Guid;

        [CanBeNull]
        public string Standort { get; set; }


        [CanBeNull]
        public string Tarif { get; set; }

        [JetBrains.Annotations.NotNull]
        public static string MakeHouseholdKey([JetBrains.Annotations.NotNull] string hComplexName,
                                              [CanBeNull] string saStandort,
                                              [JetBrains.Annotations.NotNull] string hhName)
        {
            string s = hComplexName + "###" + saStandort + "###" + hhName;
            using (SHA512Managed sha1 = new SHA512Managed()) {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }
    }
}