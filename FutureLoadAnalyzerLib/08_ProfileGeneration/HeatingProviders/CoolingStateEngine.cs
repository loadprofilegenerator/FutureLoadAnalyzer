using System;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingStateEngine {
        private readonly double _maxHouseEnergy;
        private readonly double _maxPower;
        [NotNull] private readonly Random _rnd;
        private readonly double _triggerHouseEnergy;

        private bool _isturnedOn;

        public CoolingStateEngine(double maxPower, double maxHouseEnergy, [NotNull] Random rnd, [NotNull] CoolingCalculationParameters coolPars)
        {
            _maxPower = maxPower / 4.0; //for 15 minutes
            _maxHouseEnergy = maxHouseEnergy;
            _rnd = rnd;
            _triggerHouseEnergy = maxHouseEnergy * coolPars.HouseMinimumEnergyTriggerinPercent;
        }

        public double ProvideEnergyForTimestep(double currentHouseEnergy)
        {
            if (_isturnedOn) {
                if (currentHouseEnergy > _maxHouseEnergy) {
                    _isturnedOn = false;
                    return 0;
                }

                return _maxPower;
            }

            double randomizerFactor = (_rnd.NextDouble() * 0.4) + 0.8;
            bool turnOnRandomizer = _rnd.NextDouble() < 0.9;
            if (turnOnRandomizer) {
                if (currentHouseEnergy < _triggerHouseEnergy * randomizerFactor) {
                    _isturnedOn = true;
                    return _maxPower;
                }
            }

            return 0;
        }
    }
}