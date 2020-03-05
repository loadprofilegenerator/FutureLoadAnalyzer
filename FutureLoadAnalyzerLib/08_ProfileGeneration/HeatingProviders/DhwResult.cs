using System.Collections.Generic;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class DhwResult {
        private readonly double _targetSum;

        public DhwResult(double targetSum)
        {
            _targetSum = targetSum;
            DhwEnergyDemand = new List<double>(new double[35040]);
        }


        [NotNull]
        public List<double> DhwEnergyDemand { get; set; }

        [NotNull]
        public List<DhwTurnOffReason> TurnOffReasons { get; } = new List<DhwTurnOffReason>(new DhwTurnOffReason[35040]);

        [NotNull]
        public Profile GetEnergyDemandProfile()
        {
            Profile p = new Profile("Heat pump demand", DhwEnergyDemand.AsReadOnly(), EnergyOrPower.Energy);
            return p.ScaleToTargetSum(_targetSum, p.Name, out _);
        }
    }
}