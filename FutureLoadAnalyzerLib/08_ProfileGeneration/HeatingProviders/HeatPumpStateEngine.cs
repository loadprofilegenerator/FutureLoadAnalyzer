using System;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatPumpStateEngine {
        [NotNull] private readonly HeatpumpCalculationParameters _hpPars;
        private readonly double _maxHouseEnergy;
        private readonly double _maxPower;
        [NotNull] private readonly Random _rnd;
        private readonly double _triggerHouseEnergy;

        //private int timeStepCountAlreadyRunning = 0;
        private bool _isturnedOn;

        public HeatPumpStateEngine(double maxPower, double maxHouseEnergy, [NotNull] Random rnd, [NotNull] HeatpumpCalculationParameters hpPars)
        {
            _maxPower = maxPower / 4.0; //for 15 minutes
            _maxHouseEnergy = maxHouseEnergy;
            _rnd = rnd;
            _hpPars = hpPars;
            _triggerHouseEnergy = maxHouseEnergy * _hpPars.HouseMinimumEnergyTriggerinPercent;
        }

        public double ProvideEnergyForTimestep(double currentHouseEnergy, int dayTimeStep)
        {
            if (_isturnedOn) {
                if (currentHouseEnergy > _maxHouseEnergy) {
                    _isturnedOn = false;
                    return 0;
                }

                if (!IsHeatingTime(dayTimeStep)) {
                    _isturnedOn = false;
                    return 0;
                }

                return _maxPower;
            }

            double randomizerFactor = (_rnd.NextDouble() * 0.4) + 0.8;
            bool turnOnRandomizer = _rnd.NextDouble() < 0.9;
            if (turnOnRandomizer) {
                if (currentHouseEnergy < _triggerHouseEnergy * randomizerFactor && IsHeatingTime(dayTimeStep)) {
                    _isturnedOn = true;
                    return _maxPower;
                }
            }

            return 0;
        }

        private bool IsHeatingTime(int dayTimeStep)
        {
            if (_hpPars.TimingMode == HeatPumpTimingMode.OverTheEntireDay) {
                return true;
            }

            if (_hpPars.StartingTimeStepEvenings == 0 || _hpPars.StoppingTimeStepMorning == 0) {
                return true;
            }

            if (dayTimeStep < _hpPars.StoppingTimeStepMorning) {
                return true;
            }

            if (dayTimeStep > _hpPars.StartingTimeStepEvenings) {
                return true;
            }

            return false;
        }
    }
}