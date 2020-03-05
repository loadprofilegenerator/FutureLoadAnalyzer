/*using System.Collections.Generic;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class StreetLight : IHouseComponent {
        public StreetLight([NotNull] string name,
                           double lowVoltageYearlyTotalElectricityUse,
                           double highVoltageYearlyTotalElectricityUse,
                           [NotNull] List<int> originalISNs,
                           int finalIsn,
                           [NotNull] string houseGuid,
                           [NotNull] string hausAnschlussGuid,
                           [NotNull] string sourceGuid,
                           [NotNull] string lightingProfileGuid,
                           double projectedPower,
                           [NotNull] string standort)
        {
            Name = name;
            LocalnetLowVoltageYearlyTotalElectricityUse = lowVoltageYearlyTotalElectricityUse;
            LocalnetHighVoltageYearlyTotalElectricityUse = highVoltageYearlyTotalElectricityUse;
            OriginalISNs = originalISNs;
            FinalIsn = finalIsn;
            HouseGuid = houseGuid;
            HausAnschlussGuid = hausAnschlussGuid;
            SourceGuid = sourceGuid;
            Guid = lightingProfileGuid;
            ProjectedPower = projectedPower;
            Standort = standort;
            EffectiveEnergyDemand = highVoltageYearlyTotalElectricityUse + lowVoltageYearlyTotalElectricityUse;
        }

        public double EffectiveEnergyDemand { get; }

        public EnergyType EnergyType { get; set; } = EnergyType.Other;
        public int FinalIsn { get; set; }
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Load;

        [NotNull]
        public string Guid { get; set; }

        [NotNull]
        public string HausAnschlussGuid { get; set; }

        public HouseComponentType HouseComponentType { get; set; } = HouseComponentType.StreetLight;

        [NotNull]
        public string HouseGuid { get; set; }

        public int ID { get; set; }


        [NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        public double LocalnetHighVoltageYearlyTotalElectricityUse { get; set; }
        public double LocalnetLowVoltageYearlyTotalElectricityUse { get; set; }

        [NotNull]
        public string Name { get; set; }

        [Ignore]
        [SQLite.Ignore]
        [NotNull]
        public List<int> OriginalISNs { get; set; }

        public double ProjectedPower { get; set; }

        [NotNull]
        public string SourceGuid { get; set; }

        [NotNull]
        public string Standort { get; set; }
    }
}*/