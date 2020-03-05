using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingGeneratorTest : UnitTestBase {
        public CoolingGeneratorTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunSingleAcTest()
        {
            PrepareUnitTest();
            // ReSharper disable AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, new Random());
            // ReSharper restore AssignNullToNotNullAttribute
            var dbRaw = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var temperatures = dbRaw.Fetch<TemperatureProfileImport>();

            Profile temperaturProfileHourly = new Profile(temperatures[0].Profile ?? throw new FlaException("was null"));
            //make heatpumpprofile
            var chp = new CoolingProfileGenerator(temperaturProfileHourly, 27, 24, Logger);
            Random rnd = new Random();
            var hpcp = CoolingCalculationParameters.MakeDefaults();
            const double energyuse = 20000;
            var hpr = chp.Run(hpcp, energyuse, rnd);
            hpr.GetEnergyDemandProfile().Values.Sum().Should().BeApproximately(energyuse * 0.333, 100);
            Profile houseEnergy = new Profile("House Energy", hpr.HouseEnergyTracker.AsReadOnly(), EnergyOrPower.Energy);
            Profile heatPumpEnergysupply = new Profile("Heat pump Energy supply", hpr.CoolingEnergySupply.AsReadOnly(), EnergyOrPower.Energy);
            Profile temperatureProfile15Min = new Profile("Temperatures", hpr.AvgTemperatures15Min.ToList().AsReadOnly(), EnergyOrPower.Energy);

            string file = WorkingDirectory.Combine("coolingrofile_Single.xlsx");
            List<Profile> profiles = new List<Profile>();
            profiles.Add(temperatureProfile15Min);
            profiles.Add(houseEnergy);
            profiles.Add(heatPumpEnergysupply);
            XlsxDumper.DumpProfilesToExcel(file, 2050, 15, new ProfileWorksheetContent("Sheet1", "Last", profiles));
        }
    }
}