using Common;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatingDegreeDay {
        public HeatingDegreeDay(double dailyAverageTemperatureTemperature, double heatingTemperature, double roomTemperature)
        {
            DailyAverageTemperature = dailyAverageTemperatureTemperature;
            if (dailyAverageTemperatureTemperature > heatingTemperature) {
                DegreeDays = 0;
            }
            else {
                DegreeDays = roomTemperature - dailyAverageTemperatureTemperature;
            }
        }

        public double DailyAverageTemperature { get; }

        public double DailyEnergyConsumption { get; set; }

        public double DegreeDays { get; }
        public override string ToString() => DailyAverageTemperature + " -> " + DegreeDays;
    }

    public class CoolingDegreeHour {
        public CoolingDegreeHour(double hourlyAverageTemperatureTemperature, double coolingTemperature, double roomTemperature)
        {
            if ( coolingTemperature< roomTemperature) {
                throw new FlaException("coolingTemperature needs to be bigger than room temp ");
            }
            HourlyAverageTemperature = hourlyAverageTemperatureTemperature;
            if (hourlyAverageTemperatureTemperature < coolingTemperature) {
                DegreeHours = 0;
            }
            else {
                DegreeHours = hourlyAverageTemperatureTemperature- roomTemperature;
            }
        }

        public double HourlyAverageTemperature { get;  }

        public double HourlyEnergyConsumption { get; set; }

        public double DegreeHours { get; }
        public override string ToString() => HourlyAverageTemperature + " -> " + DegreeHours;
    }
}