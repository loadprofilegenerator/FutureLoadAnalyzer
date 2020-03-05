using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class DhwCalculationParameters {
        public DhwCalculationParameters(int turnOnTimestep, int turnOffTimeStep, double turnOffTriggerLevel)
        {
            TurnOnTimestep = turnOnTimestep;
            TurnOffTimeStep = turnOffTimeStep;
            TurnOffTriggerLevel = turnOffTriggerLevel;
        }

        public int TurnOffTimeStep { get; set; }
        public double TurnOffTriggerLevel { get; }
        public int TurnOnTimestep { get; set; }

        [NotNull]
        public static DhwCalculationParameters MakeDefaults()
        {
            DhwCalculationParameters h = new DhwCalculationParameters(2 * 4, 6 * 4, 0.85);
            return h;
        }
    }
}