using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(HouseHeating))]
    [NPoco.PrimaryKey(nameof(HouseHeatingID))]
    [Table(nameof(HouseHeating))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class HouseHeating {
        public double LocalnetCombinedEnergyDemand { get; set; }
        public double EnergyDensityIndustrialApplication { get; set; }
        public double HeatingEnergyDifference { get; set; }

        [CanBeNull]
        public string HouseGuid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int HouseHeatingID { get; set; }


        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<HeatingSystemType> KantonDhwMethods { get; set; } = new List<HeatingSystemType>();

        [JetBrains.Annotations.NotNull]
        public string KantonDhwMethodsAsJson {
            get => JsonConvert.SerializeObject(KantonDhwMethods);
            set => KantonDhwMethods = JsonConvert.DeserializeObject<List<HeatingSystemType>>(value);
        }

        public double KantonHeatingEnergyDensity { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<HeatingSystemType> KantonHeatingMethods { get; set; } = new List<HeatingSystemType>();

        [JetBrains.Annotations.NotNull]
        public string KantonHeatingMethodsAsJson {
            get => JsonConvert.SerializeObject(KantonHeatingMethods);
            set => KantonHeatingMethods = JsonConvert.DeserializeObject<List<HeatingSystemType>>(value);
        }

        public double KantonTotalEnergyDemand { get; set; }

        public double LocalnetAdjustedHeatingDemand { get; set; }
        public double LocalnetFernwärmeEnergyUse { get; set; }

        public double LocalnetGasEnergyUse { get; set; }

        public double LocalnetHeatingEnergyDensity { get; set; }
        public int LocalnetHeatingSystemEntryCount { get; set; }

        public double MergedHeatingEnergyDensity { get; set; }
        public double KantonHeizungEnergyDemand { get; set; }
        public double KantonWarmwasserEnergyDemand { get; set; }
        public double MergedHeatingDemand { get; set; }
        public double KantonYearlyElectricityUseForHeatingEstimate { get; set; }

        public HeatingSystemType GetDominantDhwHeatingMethod()
        {
            var distinct = KantonDhwMethods.Distinct().ToList();
            if (distinct.Count == 1) {
                return KantonDhwMethods[0];
            }

            if (distinct.Contains(HeatingSystemType.Electricity)) {
                return HeatingSystemType.Electricity;
            }
            if (distinct.Contains(HeatingSystemType.Gas)) {
                return HeatingSystemType.Gas;
            }
            throw new FlaException("Too many");
        }
    }
}