using System.Collections.Generic;
using System.Linq;
using Common;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingDegreeProfile {
        public CoolingDegreeProfile([NotNull] Profile temperatures, double coolingTemperature, double roomTemperature)
        {
            var timeStepsPerHour = FindProfileResolution(temperatures);
            var hourlyAverages = MakeHourlyAverages(timeStepsPerHour, temperatures);
            for (var i = 0; i < hourlyAverages.Count; i++) {
                var hourlyAverage = hourlyAverages[i];
                CoolingDegreeHours.Add(new CoolingDegreeHour(hourlyAverage,
                    coolingTemperature, roomTemperature));
            }
        }

        public void InitializeDailyAmounts(double yearlyConsumption)
        {
            double sum = CoolingDegreeHours.Select(x => x.DegreeHours).Sum();
            double degreeDayProportion = yearlyConsumption / sum;
            foreach (var day in CoolingDegreeHours) {
                day.HourlyEnergyConsumption = day.DegreeHours * degreeDayProportion;
            }
        }

        [NotNull]
        [ItemNotNull]
        public List<CoolingDegreeHour> CoolingDegreeHours { get;  } = new List<CoolingDegreeHour>();

        public double CalculateHeatingDegreeDaySum()
        {
            return CoolingDegreeHours.Select(x => x.DegreeHours).Sum();
        }

        public double CalculateYearlyConsumptionSum()
        {
            return CoolingDegreeHours.Select(x => x.HourlyEnergyConsumption).Sum();
        }

        private static int FindProfileResolution([NotNull] Profile temperatures)
        {
            int timeStepsPerHour;
            if (temperatures.Values.Count == 35040) {
                timeStepsPerHour = 4 ;
            }
            else if (temperatures.Values.Count == 8760) {
                timeStepsPerHour = 1;
            }
            else {
                throw new FlaException("Invalid profile value count");
            }

            return timeStepsPerHour;
        }

        [NotNull]
        private static List<double> MakeHourlyAverages(int timeStepsPerHour, [NotNull] Profile profile)
        {
            List<double> dailyaverages;
            dailyaverages = new List<double>();
            int idx = 0;
            for (int i = 0; i < 8760; i++) {
                //days
                double dailySum = 0;
                for (int j = 0; j < timeStepsPerHour; j++) {
                    dailySum += profile.Values[idx];
                    idx++;
                }

                dailySum /= timeStepsPerHour;
                dailyaverages.Add(dailySum);
            }

            return dailyaverages;
        }
    }
}