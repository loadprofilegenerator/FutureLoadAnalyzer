using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingCalculationParameters {
        public HeatPumpTimingMode TimingMode { get; set; }
        public double TargetMaximumRuntimePerDay { get; set; }
        public double HouseMinimumEnergyTriggerinPercent { get; set; }

        public CoolingCalculationParameters(double targetMaximumRuntimePerDay, double houseMinimumEnergyTriggerinPercent)
        {
            TargetMaximumRuntimePerDay = targetMaximumRuntimePerDay;
            HouseMinimumEnergyTriggerinPercent = houseMinimumEnergyTriggerinPercent;
        }

        [NotNull]
        public static CoolingCalculationParameters MakeDefaults()
        {
            CoolingCalculationParameters h = new CoolingCalculationParameters(3,
                0.95);
            return h;
        }
    }
}