using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class HeatingProviderTest : UnitTestBase {
        public HeatingProviderTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void Run()
        {
            PrepareUnitTest();
            // ReSharper disable AssignNullToNotNullAttribute
            ServiceRepository sp = new ServiceRepository(null, null, Logger, Config, new Random());
            // ReSharper restore AssignNullToNotNullAttribute

            var utopiaSlice = new ScenarioSliceParameters(Scenario.FromEnum(ScenarioEnum.Utopia), 2050, null);
            var dbHouse = sp.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, utopiaSlice);
            var heatingSystemEntries = dbHouse.Fetch<HeatingSystemEntry>();
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();
            DBDto dbDto = new DBDto(new List<House>(), hausanschlusses, new List<Car>(), new List<Household>(), new List<RlmProfile>());
            HeatingProvider hp = new HeatingProvider(sp, utopiaSlice, dbDto);
            Info("total hse: " + heatingSystemEntries.Count);
            Profile sumProfile = Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour);
            foreach (var entry in heatingSystemEntries) {
                (entry.HouseComponentType == HouseComponentType.Heating).Should().BeTrue();
                hp.IsCorrectProvider(entry).Should().BeTrue();
                HouseComponentRo hro = new HouseComponentRo("", "", 1, 1, "", "", "", 0);
                ProviderParameterDto ppd = new ProviderParameterDto(entry, "", hro);
                hp.PrepareLoadProfileIfNeeded(ppd);
                var prof = hp.ProvideProfile(ppd);
                if (prof != null) {
                    if (prof.Profile == null) {
                        throw new FlaException("Profile was null");
                    }

                    prof.Profile.EnergySum().Should().BeApproximately(entry.EffectiveEnergyDemand / 3, entry.EffectiveEnergyDemand * 0.1);
                    sumProfile = sumProfile.Add(prof.Profile.Values);
                }
            }

            var fn = Path.Combine(WorkingDirectory.Dir, "Profiletest.xlsx");
            XlsxDumper.DumpProfilesToExcel(fn, 2050, 15, new ProfileWorksheetContent("Sum", "Last", 240, sumProfile));
        }
    }

    public class HeatingProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] private readonly DBDto _dbDto;
        [NotNull] private readonly HeatpumpProfileGenerator _hpg;

        public HeatingProvider([NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice, [NotNull] DBDto dbDto) : base(
            nameof(HeatingProvider),
            services,
            slice)
        {
            _dbDto = dbDto;
            var dbRaw = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var temperatures = dbRaw.Fetch<TemperatureProfileImport>();
            var temp = temperatures.Single(x => x.Jahr == slice.DstYear);
            Profile temperaturProfileHourly = new Profile(temp.Profile ?? throw new FlaException("Missing profile"));
            _hpg = new HeatpumpProfileGenerator(temperaturProfileHourly, 15, 20, MyLogger);
        }


        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Heating) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            HeatingSystemEntry hse = (HeatingSystemEntry)ppdto.HouseComponent;
            if (hse.HouseComponentType != HouseComponentType.Heating) {
                throw new FlaException("Wrong type");
            }

            ppdto.HouseComponentResultObject.HeatingSystemType = hse.SynthesizedHeatingSystemType;
            if (hse.SynthesizedHeatingSystemType != HeatingSystemType.Heatpump && hse.SynthesizedHeatingSystemType != HeatingSystemType.Electricity) {
                ppdto.HouseComponentResultObject.HeatingSystemMessage = "Not electric heating";
                return null;
            }

            if (!hse.ProvideProfile) {
                return null;
            }

            if (hse.HausAnschlussGuid == null) {
                return null;
            }

            Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == hse.HausAnschlussGuid);
            if (ha.ObjectID.ToLower().Contains("kleinanschluss")) {
                throw new FlaException("Heizung am kleinanschluss?");
            }

            var pa = new Prosumer(hse.HouseGuid,
                hse.Standort,
                hse.HouseComponentType,
                hse.SourceGuid,
                hse.FinalIsn,
                hse.HausAnschlussGuid,
                ha.ObjectID,
                GenerationOrLoad.Load,
                ha.Trafokreis,
                Name,
                "Heatpump Profile Generator");
            //todo: randomize this with buckets and/or simulate a central control
            int morningTime = 4 * 4 + Services.Rnd.Next(8);
            int eveningTime = 20 * 4 + Services.Rnd.Next(8);
            double targetRuntimePerDay = 16 + Services.Rnd.NextDouble() * 4;
            double trigger = 1 - Services.Rnd.NextDouble() * 0.1;
            HeatpumpCalculationParameters hpc = new HeatpumpCalculationParameters(HeatPumpTimingMode.OverTheEntireDay,
                morningTime,
                eveningTime,
                targetRuntimePerDay,
                trigger,
                1);
            hpc.StartLevelPercent = 1 - Services.Rnd.NextDouble() * .5;
            var hpr = _hpg.Run(hpc, hse.EffectiveEnergyDemand, Services.Rnd);
            pa.Profile = hpr.GetEnergyDemandProfile();
            return pa;
        }
    }
}