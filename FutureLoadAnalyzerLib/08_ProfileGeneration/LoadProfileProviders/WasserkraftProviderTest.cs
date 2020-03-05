using System;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using FluentAssertions;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class WasserkraftProviderTest : UnitTestBase {
        [Fact]
        public void Run()
        {
            // ReSharper restore AssignNullToNotNullAttribute
            // ReSharper disable twice AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null, null,
                Logger, Config, new Random(1));
            var dbRaw = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var measuredRlmProfiles = dbRaw.Fetch<RlmProfile>();

            var dbHouse = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();
            var kleinwasserkraft = dbHouse.Fetch<KleinWasserkraft>();

            var dbdto = new DBDto(null, hausanschlusses, null, null, measuredRlmProfiles);
            /*KleinWasserkraft wk =new KleinWasserkraft("name",1,1,new List<int>(){1},1,"houseguid",
                "hausanschlussguid","sourceguid","standort","geschäftspartner","bezeichnung", "anlagennummer",
                "status","lastprofil");*/
            HouseComponentRo hcro = new HouseComponentRo("myname", "PV", 1, 1, "status", "isns", "standort",0);
            WasserkraftProvider wkp = new WasserkraftProvider(services, Constants.PresentSlice, dbdto);

            foreach (var wasserkraft in kleinwasserkraft) {
                ProviderParameterDto pp = new ProviderParameterDto(wasserkraft, null, hcro);

                wkp.PrepareLoadProfileIfNeeded(pp);
                wkp.IsCorrectProvider(wasserkraft).Should().BeTrue();
                wkp.ProvideProfile(pp);
            }
        }

        public WasserkraftProviderTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}