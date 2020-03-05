using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Common;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;

namespace Data.DataModel.Creation {
    public class CarDistanceEntry : IHouseComponent {
        public CarDistanceEntry([NotNull] string houseGuid,
                                [NotNull] string householdGuid,
                                [NotNull] string carGuid,
                                double commutingDistance,
                                double freizeitDistance,
                                [NotNull] List<int> originalISNs,
                                int finalIsn,
                                [NotNull] string hausAnschlussGuid,
                                [NotNull] string cdeGuid,
                                [NotNull] string name,
                                CarType carType)
        {
            Name = name;
            CarType = carType;
            HouseGuid = houseGuid;
            if (string.IsNullOrWhiteSpace(householdGuid)) {
                throw new FlaException("household guid was null");
            }

            HouseholdGuid = householdGuid;
            CarGuid = carGuid;
            CommutingDistance = commutingDistance;
            FreizeitDistance = freizeitDistance;
            OriginalISNs = originalISNs;
            FinalIsn = finalIsn;
            HausAnschlussGuid = hausAnschlussGuid;
            SourceGuid = cdeGuid;
            Guid = cdeGuid;
            TotalDistance = commutingDistance + freizeitDistance;
            EnergyType = EnergyType.Other;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        [Obsolete("only for json")]
        public CarDistanceEntry()
        {
        }

        [NotNull]
        public string CarGuid { get; set; }

        public CarType CarType { get; set; }

        public double CommutingDistance { get; set; }

        [Ignore]
        [SQLite.Ignore]
        public double DistanceEstimate => (CommutingDistance * 250 * 2 + FreizeitDistance * 365);

        [Ignore]
        [SQLite.Ignore]
        public double EnergyEstimate => DistanceEstimate * 20 / 100.0;

        public double FreizeitDistance { get; set; }


        [NotNull]
        public string HouseholdGuid { get; set; }

        public int ID { get; set; }

        [NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        public double TotalDistance { get; set; }

        [NotNull]
        public string Guid { get; set; }

        [Ignore]
        [SQLite.Ignore]
        public double EffectiveEnergyDemand {
            get {
                if (CarType == CarType.Electric) {
                    return EnergyEstimate; //220 days, 15 kwh/100km
                }

                return 0;
            }
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        public EnergyType EnergyType { get; set; }

        public int FinalIsn { get; set; }
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        public string HausAnschlussGuid { get; set; }

        [Ignore]
        [SQLite.Ignore]
        public HouseComponentType HouseComponentType { get; } = HouseComponentType.OutboundElectricCommuter;

        [NotNull]
        public string HouseGuid { get; set; }

        public double LocalnetHighVoltageYearlyTotalElectricityUse { get; set; }
        public double LocalnetLowVoltageYearlyTotalElectricityUse { get; set; }

        public string Name { get; set; }

        [Ignore]
        [SQLite.Ignore]
        [NotNull]
        public List<int> OriginalISNs { get; set; } = new List<int>();

        public string SourceGuid { get; set; }

        [CanBeNull]
        public string Standort { get; set; }
    }
}