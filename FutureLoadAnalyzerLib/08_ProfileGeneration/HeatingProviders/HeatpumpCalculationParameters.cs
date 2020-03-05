using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatpumpCalculationParameters {
        public HeatPumpTimingMode TimingMode { get; set; }
        public int StoppingTimeStepMorning { get; set; }
        public int StartingTimeStepEvenings { get; set; }
        public double TargetMaximumRuntimePerDay { get; set; }
        public double HouseMinimumEnergyTriggerinPercent { get; set; }
        public double StartLevelPercent { get; set; }
        public double HeatPumpCop { get; set; }

        public HeatpumpCalculationParameters(HeatPumpTimingMode timingMode, int stoppingTimeStepMorning,
                                             int startingTimeStepEvenings, double targetMaximumRuntimePerDay, double houseMinimumEnergyTriggerinPercent,
                                             double heatPumpCop)
        {
            TimingMode = timingMode;
            StoppingTimeStepMorning = stoppingTimeStepMorning;
            StartingTimeStepEvenings = startingTimeStepEvenings;
            TargetMaximumRuntimePerDay = targetMaximumRuntimePerDay;
            HouseMinimumEnergyTriggerinPercent = houseMinimumEnergyTriggerinPercent;
            HeatPumpCop = heatPumpCop;
        }

        [NotNull]
        public static HeatpumpCalculationParameters MakeDefaults()
        {
            HeatpumpCalculationParameters h = new HeatpumpCalculationParameters(HeatPumpTimingMode.OnlyAtNight,
                6*4,21*4,6,0.75, 1);
            return h;
        }
    }
}