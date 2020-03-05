using System.Linq;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects {
    public class HouseComponentRo {
        public HouseComponentRo([NotNull] string name, [NotNull] string houseComponentType,
                                double lowVoltageElectricityTotal, double highVoltageElectricityTotal, [NotNull] string processingStatus,
                                [NotNull] string isns, [CanBeNull] string standort, double effectiveEnergyUse )
        {
            Isns = isns;
            Standort = standort;
            EffectiveEnergyUse = effectiveEnergyUse;
            Name = name;
            HouseComponentType = houseComponentType;
            LowVoltageElectricityTotal = lowVoltageElectricityTotal;
            HighVoltageElectricityTotal = highVoltageElectricityTotal;
            ProcessingStatus = processingStatus;
        }

        [NotNull]
        public string Name { get; }
        [NotNull]
        public string HouseComponentType { get; }
        public double LowVoltageElectricityTotal { get; }
        public double HighVoltageElectricityTotal { get; }
        [NotNull]
        public string ProcessingStatus { get; set; }
        [CanBeNull]
        public object ProfileEnergy { get; set; }

        [CanBeNull]
        public string ProfileSource { get; set; }

        public double AdjustmentFactor { get; set; }
        [NotNull]
        public string Isns { get;  }

        [CanBeNull]
        public string Standort { get;  }

        public double EffectiveEnergyUse { get; }

        [CanBeNull]
        public string UsedProvider { get; set; }

        public double CommutingDistance { get; set; }
        public double OtherDrivingDistance { get; set; }
        public double ActualDrivingDistance { get; set; }
        [CanBeNull]
        public string CarStatus { get; set; }
        [CanBeNull]
        public string ErrorMessage { get; set; }

        public HeatingSystemType HeatingSystemType { get; set; }
        [CanBeNull]
        public string HeatingSystemMessage { get; set; }

        [NotNull]
        public RowBuilder ToRowBuilder([NotNull] HouseRo house, [NotNull] HausAnschlussRo hausAnschluss, XlsResultOutputMode mode)
        {
            var rb = RowBuilder.GetAllProperties(this);
            if (mode == XlsResultOutputMode.FullLine) {
                rb.Merge(hausAnschluss.ToRowBuilder(house,mode));
            }
            return rb;
        }

        public void AddProsumerInformation([NotNull] Prosumer prosumer)
        {
            var profile = prosumer.Profile;
            if (profile == null) {
                return;
            }

            ProfileSource = prosumer.ProfileSourceName;
            ProfileEnergy = profile.EnergySum();
            MaximumPowerInkW = profile.Values.Max()*4;
            GenerationOrLoad = prosumer.GenerationOrLoad.ToString();
        }

        public double MaximumPowerInkW { get; set; }
        [CanBeNull]
        public string BusinessCategory { get; set; }
        [CanBeNull]
        public string GenerationOrLoad { get; set; }
        [CanBeNull]
        public string CoolingType { get; set; }

        [CanBeNull]
        public string DhwSystemType { get; set; }
        [CanBeNull]
        public string RlmFilename { get; set; }

        [CanBeNull] public string LPGErrors { get; set; }
    }
}