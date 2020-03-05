using System.Collections.Generic;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatPumpResult {
        public HeatPumpResult()
        {
            HeatpumpEnergyDemand = new List<double>(new double[35040]);
            HeatpumpEnergySupply = new List<double>(new double[35040]);
            HouseEnergyTracker = new List<double>(new double[35040]);
            DailyAvgTemperatures15Min = new List<double>(new double[35040]);
        }

        [NotNull]
        public List<double> DailyAvgTemperatures15Min { get; set; }


        [NotNull]
        public List<double> HeatpumpEnergyDemand { get; set; }

        [NotNull]
        public List<double> HeatpumpEnergySupply { get; set; }

        [NotNull]
        public List<double> HouseEnergyTracker { get; set; }

        [NotNull]
        public Profile GetEnergyDemandProfile()
        {
            Profile p = new Profile("Heat pump demand", HeatpumpEnergyDemand.AsReadOnly(), EnergyOrPower.Energy);
            return p;
        }
    }
}