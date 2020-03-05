using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class OverrideEntry {
        [NotNull]
        public string ComplexName { get;  }
        public HeatingSystemType HeatingSystemType { get;  }
        public EnergyDemandSource Source { get;  }

        public OverrideEntry([NotNull] string complexName, HeatingSystemType heatingSystemType, EnergyDemandSource source, double amount)
        {
            ComplexName = complexName;
            HeatingSystemType = heatingSystemType;
            Source = source;
            Amount = amount;
        }

        public double Amount { get; }
    }
}