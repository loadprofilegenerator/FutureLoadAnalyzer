using System.Collections.Generic;
using Common.Database;
using Data.DataModel.Export;
using JetBrains.Annotations;

namespace Data.DataModel.Creation {
    public interface IHouseComponent : IGuidProvider {
        double EffectiveEnergyDemand { get; }
        EnergyType EnergyType { get; }
        int FinalIsn { get; }
        GenerationOrLoad GenerationOrLoad { get; }

        [CanBeNull]
        string HausAnschlussGuid { get; }

        HouseComponentType HouseComponentType { get; }

        [NotNull]
        string HouseGuid { get; }
        double LocalnetHighVoltageYearlyTotalElectricityUse { get; }

        double LocalnetLowVoltageYearlyTotalElectricityUse { get; }

        [NotNull]
        string Name { get; set; }

        [NotNull]
        List<int> OriginalISNs { get; }

        [NotNull]
        string SourceGuid { get; }

        [CanBeNull]
        string Standort { get; }
    }
}