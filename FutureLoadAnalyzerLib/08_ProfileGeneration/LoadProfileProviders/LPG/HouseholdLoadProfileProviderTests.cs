using System;
using System.Collections.Generic;
using System.IO;
using Automation;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Common.Testing;
using Data.DataModel;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Gender = Data.DataModel.Creation.Gender;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class HouseholdLoadProfileProviderTests {
        public HouseholdLoadProfileProviderTests([CanBeNull] ITestOutputHelper output) => _output = output;

        [CanBeNull] private readonly ITestOutputHelper _output;

        [NotNull]
        private Logger InitializeTestCase([NotNull] out ScenarioSliceParameters slice,
                                          [NotNull] [ItemNotNull] out List<HouseCreationAndCalculationJob> houseJobs,
                                          [NotNull] out HouseholdLoadProfileProvider hlpp,
                                          [NotNull] out Household hh,
                                          [NotNull] out WorkingDirectory wd,
                                          [NotNull] out ServiceRepository services)
        {
            RunningConfig rc = RunningConfig.MakeDefaults();
            Logger logger = new Logger(_output, rc);
            wd = new WorkingDirectory(logger, rc);
            Random rnd = new Random();
            // ReSharper disable twice AssignNullToNotNullAttribute
            services = new ServiceRepository(null, null, logger, rc, rnd);
            List<Hausanschluss> has = new List<Hausanschluss>();
            Hausanschluss ha = new Hausanschluss("haguid",
                "houseguid",
                "bla",
                1,
                1,
                1,
                1,
                "trafokreis",
                HouseMatchingType.DirectByIsn,
                0,
                "adress",
                "standort");
            has.Add(ha);
            List<House> houses = new List<House>();
            House h = new House("housename", "complexguid", "houseguid");
            houses.Add(h);
            slice = new ScenarioSliceParameters(Scenario.Present(), 2017, null);
            houseJobs = new List<HouseCreationAndCalculationJob>();
            var dbSrcProfiles = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var vdewValues = dbSrcProfiles.Fetch<VDEWProfileValue>();
            var feiertage = dbSrcProfiles.Fetch<FeiertagImport>();
            SLPProvider slp = new SLPProvider(2017, vdewValues,feiertage);
            var cars = new List<Car>();
            DBDto dbdto = new DBDto(houses, has, cars, new List<Household>(), new List<RlmProfile>());
            CachingLPGProfileLoader clpl = new CachingLPGProfileLoader(logger, dbdto);
            hlpp = new HouseholdLoadProfileProvider(services, slice, houseJobs, slp, dbdto, clpl);
            hh = new Household("hhname", "houseguid", "haguid","hausname","standort",Guid.NewGuid().ToString());
            hh.HouseholdKey = Household.MakeHouseholdKey(h.ComplexName, ha.Trafokreis, hh.Name);
            hh.Occupants.Add(new Occupant("occguid1", 40, Gender.Male));
            return logger;
        }

        [Fact]
        public void RunHouseholdLoadProviderPreparingTest()
        {
            var logger = InitializeTestCase(out var slice, out var districts, out var hlpp, out var hh, out WorkingDirectory wd, out var _);
            HouseComponentRo hcro = new HouseComponentRo("name", "component", 1, 1, "status", "", "standort",0);
            ProviderParameterDto parameters = new ProviderParameterDto(hh, "dir", hcro);
            hlpp.PrepareLoadProfileIfNeeded(parameters);
            logger.Info(JsonConvert.SerializeObject(districts, Formatting.Indented), Stage.Testing, nameof(HouseholdLoadProfileProviderTests));
            wd.CleanAndCreate();
            string districtdirectory = Path.Combine(wd.Dir, "districts");

            DirectoryInfo districtDirectory = new DirectoryInfo(districtdirectory);
            districtDirectory.Create();

            var endTime = new DateTime(slice.DstYear, 1, 31);

            HouseProcessor.WriteDistrictsForLPG(districts,
                districtDirectory,
                logger,
                Constants.PresentSlice,
                endTime,
                new ProfileGenerationRo());
        }

        [Fact]
        public void RunHouseholdLoadProviderProvidingTest()
        {
            InitializeTestCase(out var _, out var _, out var hlpp, out var hh, out WorkingDirectory wd, out var _);
            HouseComponentRo hcro = new HouseComponentRo("name", "type", 1, 1, "status", "", "standort",0);
            ProviderParameterDto parameters = new ProviderParameterDto(hh, wd.Dir, hcro);
            var profile = hlpp.ProvideProfile(parameters);
            Assert.NotNull(profile);
            _output?.WriteLine("Total values in profile: " + profile.Profile?.Values.Count);
            Assert.True(profile.Profile?.Values.Count > 0);
        }
    }
}