using System;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public enum DhwTurnOffReason {
        None,
        ExceedRuntime,
        DailyEnergyExceeded,
        HeatingTimePassed,
        RandomTrigger
    }

    public class DhwStateEngine {
        private readonly double _dailyEnergy;
        [NotNull] private readonly DhwCalculationParameters _dhwPars;
        [NotNull] private readonly Random _rnd;

        private readonly double _targetPower;
        private readonly int _targetRuntime;
        private double _dayEnergyAlready;
        private bool _isturnedOn;

        private int _turnedOnSteps;

        public DhwStateEngine(double yearlyEnergy, [NotNull] DhwCalculationParameters dhwPars, [NotNull] Random rnd)
        {
            _dhwPars = dhwPars;
            _rnd = rnd;
            _targetRuntime = dhwPars.TurnOffTimeStep - dhwPars.TurnOnTimestep;
            _targetPower = yearlyEnergy / 365 / (_targetRuntime - 2);
            _dailyEnergy = yearlyEnergy / 365 * 1.1;
        }

        public double ProvideEnergyForTimestep(int dayTimeStep, out DhwTurnOffReason reason)
        {
            if (dayTimeStep == 0) {
                _dayEnergyAlready = 0;
            }

            if (_isturnedOn) {
                _turnedOnSteps++;
                if (_turnedOnSteps > _targetRuntime) {
                    _isturnedOn = false;
                    reason = DhwTurnOffReason.ExceedRuntime;
                    return 0;
                }

                if (_dayEnergyAlready > _dailyEnergy) {
                    reason = DhwTurnOffReason.DailyEnergyExceeded;
                    _isturnedOn = false;
                    return 0;
                }

                if (!IsHeatingTime(dayTimeStep)) {
                    reason = DhwTurnOffReason.HeatingTimePassed;
                    _isturnedOn = false;
                    return 0;
                }

                if (_rnd.NextDouble() > _dhwPars.TurnOffTriggerLevel) {
                    reason = DhwTurnOffReason.RandomTrigger;
                    _isturnedOn = false;
                    return 0;
                }

                _dayEnergyAlready += _targetPower;
                reason = DhwTurnOffReason.None;
                return _targetPower;
            }

            if (IsHeatingTime(dayTimeStep) && Math.Abs(_dayEnergyAlready) < 0.0001) {
                _turnedOnSteps = 0;
                _isturnedOn = true;
                _dayEnergyAlready += _targetPower;
                reason = DhwTurnOffReason.None;
                return _targetPower;
            }

            reason = DhwTurnOffReason.None;
            return 0;
        }

        private bool IsHeatingTime(int dayTimeStep)
        {
            if (dayTimeStep <= _dhwPars.TurnOffTimeStep && dayTimeStep >= _dhwPars.TurnOnTimestep) {
                return true;
            }

            return false;
        }
    }
}