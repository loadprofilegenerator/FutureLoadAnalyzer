using System.Collections.Generic;
using System.Linq;
using Common;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatingDegreeProfile {
        public HeatingDegreeProfile([NotNull] Profile temperatures, double heatingTemperature, double roomTemperature)
        {
            var timeStepsPerDay = FindProfileResolution(temperatures);
            var dailyaverages = MakeDailyAverages(timeStepsPerDay, temperatures);

            for (var i = 0; i < dailyaverages.Count; i++) {
                var dailyaverage = dailyaverages[i];
                HeatingDegreeDays.Add(new HeatingDegreeDay(dailyaverage, heatingTemperature, roomTemperature));
            }
        }

        public void InitializeDailyAmounts(double yearlyConsumption)
        {
            double sum = HeatingDegreeDays.Select(x => x.DegreeDays).Sum();
            double degreeDayProportion = yearlyConsumption / sum;
            foreach (var day in HeatingDegreeDays) {
                day.DailyEnergyConsumption = day.DegreeDays * degreeDayProportion;
            }
        }

        [NotNull]
        [ItemNotNull]
        public List<HeatingDegreeDay> HeatingDegreeDays { get;  } = new List<HeatingDegreeDay>();

        public double CalculateHeatingDegreeDaySum()
        {
            return HeatingDegreeDays.Select(x => x.DegreeDays).Sum();
        }

        public double CalculateYearlyConsumptionSum()
        {
            return HeatingDegreeDays.Select(x => x.DailyEnergyConsumption).Sum();
        }

        private static int FindProfileResolution([NotNull] Profile temperatures)
        {
            int timeStepsPerDay;
            if (temperatures.Values.Count == 35040) {
                timeStepsPerDay = 4 * 24;
            }
            else if (temperatures.Values.Count == 8760) {
                timeStepsPerDay = 24;
            }
            else {
                throw new FlaException("Invalid profile value count");
            }

            return timeStepsPerDay;
        }

        [NotNull]
        private static List<double> MakeDailyAverages(int timeStepsPerDay, [NotNull] Profile profile)
        {
            List<double> dailyaverages;
            dailyaverages = new List<double>();
            int idx = 0;
            for (int i = 0; i < 365; i++) {
                //days
                double dailySum = 0;
                for (int j = 0; j < timeStepsPerDay; j++) {
                    dailySum += profile.Values[idx];
                    idx++;
                }

                dailySum /= timeStepsPerDay;
                dailyaverages.Add(dailySum);
            }

            return dailyaverages;
        }
    }
}