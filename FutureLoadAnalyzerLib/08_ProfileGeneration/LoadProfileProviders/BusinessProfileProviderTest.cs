using System;
using System.Collections.Generic;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    /// <summary>
    ///     make the business profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BusinessProfileProviderTest : UnitTestBase {
        [Fact]
        public void RunTest()
        {
            Random rnd = new Random();
            // ReSharper disable twice AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, rnd);
            var dbSrcProfiles = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var vdewvals = dbSrcProfiles.Fetch<VDEWProfileValue>();
            var feiertage = dbSrcProfiles.Fetch<FeiertagImport>();
            SLPProvider slp = new SLPProvider(2017, vdewvals, feiertage);
            DBDto dbDto = new DBDto(new List<House>(), new List<Hausanschluss>(), new List<Car>(), new List<Household>(), new List<RlmProfile>());
            BusinessProfileProvider bpp = new BusinessProfileProvider(services, Constants.PresentSlice, slp, dbDto);
            BusinessEntry be = new BusinessEntry(Guid.NewGuid().ToString(), "businessname", BusinessType.Brauerei, "housename");
            HouseComponentRo hcro = new HouseComponentRo("name", "type", 1, 1, "status", "", "standort", 0);
            ProviderParameterDto parameeters = new ProviderParameterDto(be, "", hcro);
            bpp.PrepareLoadProfileIfNeeded(parameeters);
            var prosumer = bpp.ProvideProfile(parameeters);
            Assert.NotNull(prosumer);
        }

        public BusinessProfileProviderTest([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}