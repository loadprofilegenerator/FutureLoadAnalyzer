using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName(nameof(HeatingSystemEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(HeatingSystemEntry))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class HeatingSystemEntry : IHouseComponent {
        private int _age;
        private HeatingSystemType _synthesizedHeatingSystemType;

        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public HeatingSystemEntry()
        {
        }

        public HeatingSystemEntry([JetBrains.Annotations.NotNull] string houseGuid,
                                  [JetBrains.Annotations.NotNull] string heatingSystemGuid,
                                  [JetBrains.Annotations.NotNull] string feuerungsstättenType,
                                  [CanBeNull] string hausAnschlussGuid,
                                  [JetBrains.Annotations.NotNull] string name,
                                  [JetBrains.Annotations.NotNull] string standort)
        {
            HouseGuid = houseGuid;
            Guid = heatingSystemGuid;
            FeuerungsstättenType = feuerungsstättenType;
            HausAnschlussGuid = hausAnschlussGuid;
            Name = name;
            Standort = standort;
        }

        public int Age {
            get => _age;
            set {
                _age = value;
                if (value > 200) {
                    throw new FlaException("Age > 200 years");
                }
            }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public double CalculatedAverageHeatingEnergyDemandDensity {
            get {
                double totalEbf = HeatDemands.Sum(x => x.HeatDemand);
                if (Math.Abs(totalEbf) < 0.0001) {
                    totalEbf = HeatDemand / 250; // assume 250 kWh/m2/a
                }

                return HeatDemand / totalEbf;
            }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public double? CalculatedAverageHeatingEnergyDemandDensityWithNulls {
            get {
                double totalEbf = HeatDemands.Sum(x => x.HeatDemand);
                if (Math.Abs(totalEbf) < 0.0001) {
                    return null;
                }

                return HeatDemand / totalEbf;
            }
        }

        public double EstimatedMaximumEnergyFromFeuerungsStätten { get; set; }

        public double EstimatedMinimumEnergyFromFeuerungsStätten { get; set; }
        public double FeuerungsstättenPower { get; set; }

        [JetBrains.Annotations.NotNull]
        public string FeuerungsstättenType { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public double HeatDemand {
            get { return HeatDemands.Sum(x => x.HeatDemand); }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public double Ebf {
            get { return HeatDemands.Sum(x => x.Energiebezugsfläche); }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<AppartmentHeatingDemand> HeatDemands { get; set; } = new List<AppartmentHeatingDemand>();

        [JetBrains.Annotations.NotNull]
        public string HeatDemandsAsJson {
            get => JsonConvert.SerializeObject(HeatDemands, Formatting.Indented);
            set => HeatDemands = JsonConvert.DeserializeObject<List<AppartmentHeatingDemand>>(value);
        }

        public HeatingSystemType HeatingSystemType2017 { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        public double OriginalHeatDemand2017 { get; set; }

        public HeatingSystemType OriginalHeatingSystemType { get; set; }

        public bool ProvideProfile { get; set; } = true;

        public HeatingSystemType SynthesizedHeatingSystemType {
            get => _synthesizedHeatingSystemType;
            set {
                _synthesizedHeatingSystemType = value;
                if (_synthesizedHeatingSystemType == HeatingSystemType.Electricity) {
                    EnergyType = EnergyType.Electricity;
                    return;
                }

                if (_synthesizedHeatingSystemType == HeatingSystemType.Heatpump) {
                    EnergyType = EnergyType.Electricity;
                    return;
                }

                if (_synthesizedHeatingSystemType == HeatingSystemType.Gas) {
                    EnergyType = EnergyType.Gas;
                    return;
                }

                if (_synthesizedHeatingSystemType == HeatingSystemType.Öl) {
                    EnergyType = EnergyType.Oil;
                    return;
                }

                EnergyType = EnergyType.Other;
            }
        }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public double EffectiveEnergyDemand {
            get {
                if (SynthesizedHeatingSystemType == HeatingSystemType.Heatpump) {
                    return HeatDemand / 3;
                }

                return HeatDemand;
            }

            // ReSharper disable once ValueParameterNotUsed
        }

        public EnergyType EnergyType { get; set; }

        public int FinalIsn { get; set; }
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        [CanBeNull]
        public string HausAnschlussGuid { get; set; }

        public HouseComponentType HouseComponentType { get; set; } = HouseComponentType.Heating;

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

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }

        public void RenovateAllHeatDemand(double averageRenovationFactor)
        {
            foreach (var demand in HeatDemands) {
                demand.HeatDemand *= averageRenovationFactor;
            }
        }
    }
}