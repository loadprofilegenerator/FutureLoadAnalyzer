using System;
using System.Collections.Generic;
using System.Linq;
using Automation;
using Common.Config;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class ElectricCarProfileProviderTests : UnitTestBase {
        public ElectricCarProfileProviderTests([CanBeNull] ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void RunElectricCarProviderProvidingTest()
        {
            Config.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            Config.LimitToYears.Add(2050);
            Config.InitializeSlices(Logger);
            Config.LpgPrepareMode = LpgPrepareMode.PrepareWithFullLpgLoad;
            var slice = (Config.Slices ?? throw new InvalidOperationException()).First(x =>
                x.DstYear == 2050 && x.DstScenario == Scenario.FromEnum(ScenarioEnum.Utopia));

            // ReSharper disable twice AssignNullToNotNullAttribute
            var services = new ServiceRepository(null, null, Logger, Config, new Random());
            var dbHouses = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouses.Fetch<House>();
            var has = dbHouses.Fetch<Hausanschluss>();
            var households = dbHouses.Fetch<Household>();
            var cdes = dbHouses.Fetch<CarDistanceEntry>();
            //SLPProvider slp = new SLPProvider(2017, vdewValues, feiertage);
            var cars = dbHouses.Fetch<Car>();
            DBDto dbdto = new DBDto(houses, has, cars, households, new List<RlmProfile>());
            CachingLPGProfileLoader clpl = new CachingLPGProfileLoader(Logger, dbdto);
            ElectricCarProvider ecp = new ElectricCarProvider(services, slice, dbdto, new List<HouseCreationAndCalculationJob>(), clpl);
            double sumenergyEstimates = 0;
            double kilometers = 0;
            double sumenergyProfiles = 0;
            double carCount = 0;
            int gascars = 0;
            int evs = 0;
            int count = 0;
            foreach (var carDistanceEntry in cdes) {
                var car = cars.Single(x => x.Guid == carDistanceEntry.CarGuid);
                if (car.CarType == CarType.Electric) {
                    evs++;
                }
                else {
                    gascars++;
                }

                HouseComponentRo hcro = new HouseComponentRo(carDistanceEntry.Name,
                    carDistanceEntry.HouseComponentType.ToString(),
                    0,
                    0,
                    "",
                    carDistanceEntry.ISNsAsJson,
                    carDistanceEntry.Standort,
                    carDistanceEntry.EffectiveEnergyDemand);
                ProviderParameterDto ppd = new ProviderParameterDto(carDistanceEntry, Config.Directories.CalcServerLpgDirectory, hcro);
                ecp.PrepareLoadProfileIfNeeded(ppd);
                var prosumer = ecp.ProvideProfile(ppd);
                if (prosumer != null && prosumer.Profile != null) {
                    double energyEstimate = carDistanceEntry.EnergyEstimate;
                    sumenergyEstimates += energyEstimate;
                    kilometers += carDistanceEntry.DistanceEstimate;
                    double profileEnergy = prosumer.Profile.EnergySum();
                    sumenergyProfiles += profileEnergy;
                    carCount++;
                }

                count++;
                if (count % 100 == 0) {
                    Info("Processed " + count + " / " + cdes.Count);
                }

                //profileEnergy.Should().BeInRange(energyEstimate, energyEstimate * 1.5);
            }

            double avgKilometers = kilometers / carCount;
            Info("gasoline cars: " + gascars);
            Info("ev cars: " + evs);
            Info("EnergyEstimateSum: " + sumenergyEstimates);
            Info("ProfileSum: " + sumenergyProfiles);
            Info("cars profiles made for " + carCount + " / " + cdes.Count);
            Info("Avg km per car: " + avgKilometers);
            Info("Avg Energy estimate per car: " + sumenergyEstimates / carCount);
            Info("Avg Energy profile per car: " + sumenergyProfiles / carCount);
        }
    }
}