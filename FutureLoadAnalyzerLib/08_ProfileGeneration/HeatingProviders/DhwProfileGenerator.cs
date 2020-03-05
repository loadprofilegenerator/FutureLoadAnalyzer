using System;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class DhwProfileGenerator {
        [NotNull]
        public DhwResult Run([NotNull] DhwCalculationParameters hpPar, double yearlyConsumption, [NotNull] Random rnd)
        {
            var hpr = new DhwResult(yearlyConsumption);
            DhwStateEngine dhwStateEngine = new DhwStateEngine(yearlyConsumption, hpPar, rnd);
            int idx = 0;
            for (int i = 0; i < 365; i++) {
                for (int dayTimeStep = 0; dayTimeStep < 96; dayTimeStep++) {
                    double dhwEnergy = dhwStateEngine.ProvideEnergyForTimestep(dayTimeStep, out var reason);
                    hpr.DhwEnergyDemand[idx] = dhwEnergy;
                    hpr.TurnOffReasons[idx] = reason;
                    idx++;
                }
            }

            return hpr;
        }
    }
}