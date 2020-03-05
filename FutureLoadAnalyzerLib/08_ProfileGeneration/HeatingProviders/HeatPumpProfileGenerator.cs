using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common;
using Common.Logging;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatPumpTest : UnitTestBase {
        public HeatPumpTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private void WriteProfilesToExcel([NotNull] Profile temperaturProfile,
                                          [NotNull] Profile houseEnergy,
                                          [NotNull] Profile heatPumpEnergysupply,
                                          [NotNull] string file)
        {
            //dump to csv
            RowCollection rc = new RowCollection("sheet1", "Sheet1");
            DateTime dt = new DateTime(2017, 1, 1);
            for (int i = 0; i < 35040; i++) {
                RowBuilder rb = RowBuilder.Start("Idx", i);
                rb.Add("Time", dt.ToShortDateString() + " " + dt.ToShortTimeString());
                dt = dt.AddMinutes(15);
                rb.Add(temperaturProfile.Name, temperaturProfile.Values[i]);
                rb.Add(houseEnergy.Name, houseEnergy.Values[i]);
                rb.Add(heatPumpEnergysupply.Name, heatPumpEnergysupply.Values[i]);
                rc.Add(rb);
            }

            var fn = Path.Combine(Config.Directories.UnitTestingDirectory, nameof(HeatPumpTest), file);
            FileInfo fi = new FileInfo(fn);
            DirectoryInfo di = fi.Directory;
            if (di == null) {
                throw new FlaException("No path");
            }

            if (!di.Exists) {
                di.Create();
            }

            XlsxDumper.WriteToXlsx(fn, rc);
            Info("Wrote results to " + fn);
            Process.Start(di.FullName);
        }

        [Fact]
        public void RunMultiHpTest()
        {
            PrepareUnitTest();
            // ReSharper disable twice AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, new Random());
            var dbRaw = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouse = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses,
                new ScenarioSliceParameters(Scenario.FromEnum(ScenarioEnum.Utopia), 2050, null));
            var temperatures = dbRaw.Fetch<TemperatureProfileImport>();
            var heatPumpEntries = dbHouse.Fetch<HeatingSystemEntry>();
            Profile temperaturProfileHourly = new Profile(temperatures[0].Profile ?? throw new FlaException("was null"));
            Random rnd = new Random();
            //make heatpumpprofile
            Profile houseEnergy = Profile.MakeConstantProfile(0, "House Energy", Profile.ProfileResolution.QuarterHour);
            Profile heatPumpEnergysupply = Profile.MakeConstantProfile(0, "Heat pump Energy supply", Profile.ProfileResolution.QuarterHour);
            Profile heatPumpEnergyDemand = Profile.MakeConstantProfile(0, "Heat pump Energy Demand", Profile.ProfileResolution.QuarterHour);
            Profile temperatureProfile15Min = null;
            List<Profile> allHouseDemandProfiles = new List<Profile>();
            foreach (var entry in heatPumpEntries) {
                HeatpumpProfileGenerator chp = new HeatpumpProfileGenerator(temperaturProfileHourly, 15, 20, Logger);
                var hpcp = HeatpumpCalculationParameters.MakeDefaults();
                hpcp.StartingTimeStepEvenings = (21 * 4) + rnd.Next(12);
                hpcp.StoppingTimeStepMorning = (4 * 4) + rnd.Next(12);
                hpcp.HouseMinimumEnergyTriggerinPercent = 0.70 - rnd.NextDouble() * 0.2;
                hpcp.TargetMaximumRuntimePerDay = 16 + rnd.NextDouble() * 4;
                hpcp.TimingMode = HeatPumpTimingMode.OverTheEntireDay;
                hpcp.StartLevelPercent = 1 - rnd.NextDouble() * .5;
                HeatPumpResult hpr = chp.Run(hpcp, entry.EffectiveEnergyDemand, rnd);
                var result = hpr.GetEnergyDemandProfile();
                result.Name += entry.Standort;
                allHouseDemandProfiles.Add(result);
                result.Values.Sum().Should().BeApproximately(entry.EffectiveEnergyDemand * .333, entry.EffectiveEnergyDemand * 0.1);
                heatPumpEnergyDemand = heatPumpEnergyDemand.Add(result, heatPumpEnergyDemand.Name);
                houseEnergy = houseEnergy.Add(hpr.HouseEnergyTracker.AsReadOnly());
                heatPumpEnergysupply = heatPumpEnergysupply.Add(hpr.HeatpumpEnergySupply.AsReadOnly());
                if (temperatureProfile15Min == null) {
                    temperatureProfile15Min = new Profile("Temperatures", hpr.DailyAvgTemperatures15Min.ToList().AsReadOnly(), EnergyOrPower.Energy);
                }
            }

            allHouseDemandProfiles.Sort((x, y) => y.EnergySum().CompareTo(x.EnergySum()));
            var profilesToShow = allHouseDemandProfiles.Take(5).ToList();
            var profilesToMerge = allHouseDemandProfiles.Skip(6).ToList();
            var mergedProfiles = allHouseDemandProfiles[5].Add(profilesToMerge, "MergedProfiles");
            if (temperatureProfile15Min == null) {
                throw new FlaException("no temperatures");
            }

            ProfileWorksheetContent biggestConsumers = new ProfileWorksheetContent("Biggest Consumers", "Last", profilesToShow);
            biggestConsumers.Profiles.Add(mergedProfiles);
            string fullFileName = WorkingDirectory.Combine("heatpump_profiles_multi-WithChart.xlsx");
            XlsxDumper.DumpProfilesToExcel(fullFileName,
                2017,
                15,
                new ProfileWorksheetContent("Energy Demand", "Last", 240, heatPumpEnergyDemand),
                new ProfileWorksheetContent("Energy Supply", "Energieversorgung", 240, heatPumpEnergysupply),
                new ProfileWorksheetContent("Temperatures", "Temperatur", 240, temperatureProfile15Min),
                new ProfileWorksheetContent("House Energy", "Haus Energiegehalt", 240, houseEnergy),
                biggestConsumers);
            //WriteProfilesToExcel(temperatureProfile15Min, houseEnergy, heatPumpEnergysupply,file);
        }

        [Fact]
        public void RunSingleHpTest()
        {
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, new Random());
            var dbRaw = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var temperatures = dbRaw.Fetch<TemperatureProfileImport>();

            Profile temperaturProfileHourly = new Profile(temperatures[0].Profile ?? throw new FlaException("was null"));
            //make heatpumpprofile
            HeatpumpProfileGenerator chp = new HeatpumpProfileGenerator(temperaturProfileHourly, 15, 20, Logger);
            Random rnd = new Random();
            var hpcp = HeatpumpCalculationParameters.MakeDefaults();
            const double energyuse = 20000;
            var hpr = chp.Run(hpcp, energyuse, rnd);
            hpr.GetEnergyDemandProfile().Values.Sum().Should().BeApproximately(energyuse * 0.333, 100);
            Profile houseEnergy = new Profile("House Energy", hpr.HouseEnergyTracker.AsReadOnly(), EnergyOrPower.Energy);
            Profile heatPumpEnergysupply = new Profile("Heat pump Energy supply", hpr.HeatpumpEnergySupply.AsReadOnly(), EnergyOrPower.Energy);
            Profile temperatureProfile15Min = new Profile("Temperatures", hpr.DailyAvgTemperatures15Min.ToList().AsReadOnly(), EnergyOrPower.Energy);
            const string file = "heatpumprofile_Single.xlsx";
            WriteProfilesToExcel(temperatureProfile15Min, houseEnergy, heatPumpEnergysupply, file);
        }
    }

    // asdf
    //todo: calculate heat pump energy consumption
    // export to csv
    // do provider
    // return prosumer
    //register in house collector
    //play with parameters

    public class HeatpumpProfileGenerator {

        [NotNull] private readonly HeatingDegreeProfile _hdp;

        public HeatpumpProfileGenerator([NotNull] Profile temperatures, double heatingTemperature, double roomTemperature, [NotNull] ILogger logger)
        {
            if (temperatures.EnergyOrPower != EnergyOrPower.Temperatures) {
                throw new FlaException("Not a temperature profile");
            }

            _hdp = new HeatingDegreeProfile(temperatures, heatingTemperature, roomTemperature);
            logger.Info("Initalized the heat pump profile generator", Stage.ProfileGeneration, nameof(HeatpumpProfileGenerator));
        }

        [NotNull]
        public HeatPumpResult Run([NotNull] HeatpumpCalculationParameters hpPar, double yearlyConsumption, [NotNull] Random rnd)
        {
            var hpr = new HeatPumpResult();
            _hdp.InitializeDailyAmounts(yearlyConsumption);
            //calculate required power
            if (hpPar.HouseMinimumEnergyTriggerinPercent > 1) {
                throw new FlaException("More than 100% trigger is not possible");
            }

            var maxDailyNeed = _hdp.HeatingDegreeDays.Select(x => x.DailyEnergyConsumption).Max();
            var power = maxDailyNeed / hpPar.TargetMaximumRuntimePerDay; //only target running for 12h
            power = Math.Max(power, 5); // minimum 5kw
            power = ((int)power / 5) * 5; // round to the neared 5kw
            double houseEnergy = maxDailyNeed * hpPar.StartLevelPercent;
            int idx = 0;
            HeatPumpStateEngine hpse = new HeatPumpStateEngine(power, maxDailyNeed, rnd, hpPar);
            int prepSteps = 1000 + rnd.Next(1000);
            double dailyEnergyLoss = _hdp.HeatingDegreeDays[0].DailyEnergyConsumption / 96;
            for (int i = 0; i < prepSteps; i++) {
                houseEnergy -= dailyEnergyLoss;
                houseEnergy += hpse.ProvideEnergyForTimestep(houseEnergy, 1);
            }

            for (int i = 0; i < 365; i++) {
                double daily = _hdp.HeatingDegreeDays[i].DailyEnergyConsumption;
                double energyLostPerTimestep = daily / 96;
                for (int dayTimeStep = 0; dayTimeStep < 96; dayTimeStep++) {
                    hpr.DailyAvgTemperatures15Min[idx] = _hdp.HeatingDegreeDays[i].DailyAverageTemperature;
                    hpr.HouseEnergyTracker[idx] = houseEnergy;
                    houseEnergy -= energyLostPerTimestep;
                    double heatPumpSuppliedEnergy = hpse.ProvideEnergyForTimestep(houseEnergy, dayTimeStep);
                    //      totalEnergy += heatPumpSuppliedEnergy;
                    houseEnergy += heatPumpSuppliedEnergy;
                    hpr.HeatpumpEnergySupply[idx] = heatPumpSuppliedEnergy;
                    idx++;
                }
            }

            CalculateEnergyConsumption(hpr, hpPar.HeatPumpCop);
            return hpr;
        }

        private static void CalculateEnergyConsumption([NotNull] HeatPumpResult hpr, double heatpumpCop)
        {
            for (int i = 0; i < hpr.HeatpumpEnergySupply.Count; i++) {
                hpr.HeatpumpEnergyDemand[i] = hpr.HeatpumpEnergySupply[i] / heatpumpCop;
            }
        }
    }
}