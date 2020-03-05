using System;
using System.Collections.Generic;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public class PVProfileProviderTests : UnitTestBase {
        [Fact]
        public void RunPVProviderTest()
        {
            // ReSharper disable AssignNullToNotNullAttribute
            var dbdto = new DBDto(null, null, null, null, null);
            // ReSharper restore AssignNullToNotNullAttribute
            // ReSharper disable twice AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, new Random(1));
            PVProfileProvider pvp = new PVProfileProvider(services, Constants.PresentSlice, dbdto);
            PvSystemEntry pve = new PvSystemEntry("houseguid", "pvguid", "haguid", "myname", "pv123", 2017);
            pve.PVAreas = new List<PVSystemArea> {
                new PVSystemArea(30, 30, 1000)
            };
            HouseComponentRo hcro = new HouseComponentRo("myname", "PV", 1, 1, "status", "isns", "standort", 0);
            ProviderParameterDto pp = new ProviderParameterDto(pve, null, hcro);
            pvp.PrepareLoadProfileIfNeeded(pp);
        }

        [Fact]
        public void RunPVProviderTestForAllEntries()
        {
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, new Random(1));
            var slice = new ScenarioSliceParameters(Scenario.FromEnum(ScenarioEnum.Utopia), 2050, null);
            var dbHouse = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var pventries = dbHouse.Fetch<PvSystemEntry>();
            var has = dbHouse.Fetch<Hausanschluss>();
            var dbdto = new DBDto(null, has, null, null, null);
            PVProfileProvider pvp = new PVProfileProvider(services, slice, dbdto);
            foreach (var entry in pventries) {
                HouseComponentRo hcro = new HouseComponentRo("myname", "PV", 1, 1, "status", "isns", "standort", 0);
                ProviderParameterDto pp = new ProviderParameterDto(entry, null, hcro);
                pvp.PrepareLoadProfileIfNeeded(pp);
                var prosumer = pvp.ProvideProfile(pp);
                if (prosumer?.Profile == null) {
                    throw new FlaException("No profile");
                }

                if (Math.Abs(prosumer.Profile.EnergySum() - entry.EffectiveEnergyDemand) > 0.1) {
                    throw new FlaException("Invalid profile: missing energy: should be " + entry.EffectiveEnergyDemand + " but was " +
                                           prosumer.Profile.EnergySum());
                }

                Info("Profile generated correctly.");
            }
        }

        public PVProfileProviderTests([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}