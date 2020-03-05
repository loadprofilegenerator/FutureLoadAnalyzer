using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Logging;
using Common.Steps;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    // asdf
    //todo: calculate heat pump energy consumption
    // export to csv
    // do provider
    // return prosumer
    //register in house collector
    //play with parameters
    public class CoolingResult {
        public CoolingResult()
        {
            CoolingEnergyDemand = new List<double>(new double[35040]);
            CoolingEnergySupply = new List<double>(new double[35040]);
            HouseEnergyTracker = new List<double>(new double[35040]);
            AvgTemperatures15Min = new List<double>(new double[35040]);
        }

        [NotNull]
        public List<double> AvgTemperatures15Min { get; set; }


        [NotNull]
        public List<double> CoolingEnergyDemand { get; set; }

        [NotNull]
        public List<double> CoolingEnergySupply { get; set; }

        [NotNull]
        public List<double> HouseEnergyTracker { get; set; }

        [NotNull]
        public Profile GetEnergyDemandProfile()
        {
            Profile p = new Profile("Heat pump demand", CoolingEnergyDemand.AsReadOnly(), EnergyOrPower.Energy);
            return p;
        }
    }

    public class CoolingProfileGenerator {
        //private void Make15MinTemperatureProfile([NotNull] Profile temperatures)
        //{
        //    _temperaturesWith15Min = new List<double>(new double[35040]);
        //    for (int i = 0; i < temperatures.Values.Count;i++) {
        //        for (int j = 0; j < 4; j++) {
        //            _temperaturesWith15Min[i * 4 + j] = temperatures.Values[i];
        //        }
        //    }
        //}

        //private List<double> _temperaturesWith15Min;
        [NotNull] private readonly CoolingDegreeProfile _hdp;
        [NotNull] private readonly ILogger _logger;

        public CoolingProfileGenerator([NotNull] Profile temperatures, double coolingTemperature, double roomTemperature, [NotNull] ILogger logger)
        {
            if (temperatures.EnergyOrPower != EnergyOrPower.Temperatures) {
                throw new FlaException("Not a temperature profile");
            }

            _logger = logger;
            _hdp = new CoolingDegreeProfile(temperatures, coolingTemperature, roomTemperature);
        }

        [NotNull]
        public CoolingResult Run([NotNull] CoolingCalculationParameters hpPar, double yearlyConsumption, [NotNull] Random rnd)
        {
            var hpr = new CoolingResult();
            _hdp.InitializeDailyAmounts(yearlyConsumption);
            //calculate required power
            if (hpPar.HouseMinimumEnergyTriggerinPercent > 1) {
                throw new FlaException("More than 100% trigger is not possible");
            }

            var maxDailyNeed = _hdp.CoolingDegreeHours.Select(x => x.HourlyEnergyConsumption).Max();
            var power = maxDailyNeed / hpPar.TargetMaximumRuntimePerDay; //only target running for 12h
            power = Math.Max(power, 2); // minimum 2kw
            power = ((int)power / 2) * 2; // round to the neared 2kw
            double houseEnergy = maxDailyNeed;
            int idx = 0;
            double totalEnergy = 0;
            CoolingStateEngine hpse = new CoolingStateEngine(power, maxDailyNeed, rnd, hpPar);
            for (int i = 0; i < 8760; i++) {
                double daily = _hdp.CoolingDegreeHours[i].HourlyEnergyConsumption;
                double energyLostPerTimestep = daily / 4;
                for (int quarterhourStep = 0; quarterhourStep < 4; quarterhourStep++) {
                    hpr.AvgTemperatures15Min[idx] = _hdp.CoolingDegreeHours[i].HourlyAverageTemperature;
                    hpr.HouseEnergyTracker[idx] = houseEnergy;
                    houseEnergy -= energyLostPerTimestep;
                    double heatPumpSuppliedEnergy = hpse.ProvideEnergyForTimestep(houseEnergy);
                    totalEnergy += heatPumpSuppliedEnergy;
                    houseEnergy += heatPumpSuppliedEnergy;
                    hpr.CoolingEnergySupply[idx] = heatPumpSuppliedEnergy;
                    idx++;
                }
            }

            CalculateEnergyConsumption(hpr);
            _logger.Debug("Calculated air conditioning profile for " + yearlyConsumption + " Energy consumption in profile: " +
                          hpr.CoolingEnergySupply.Sum() + " energy demand: " + hpr.CoolingEnergySupply.Sum() + " Degree days: " +
                          _hdp.CoolingDegreeHours.Sum(x => x.DegreeHours) + " total need in degree days: " +
                          _hdp.CoolingDegreeHours.Sum(x => x.HourlyEnergyConsumption) + " Total energy: " + totalEnergy,
                Stage.ProfileGeneration,
                "Profile");
            return hpr;
        }

        private static void CalculateEnergyConsumption([NotNull] CoolingResult hpr)
        {
            for (int i = 0; i < hpr.CoolingEnergySupply.Count; i++) {
                hpr.CoolingEnergyDemand[i] = hpr.CoolingEnergySupply[i]; // no factor since cooling is just used energy
            }
        }
    }
}