using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public class SavableEntryTester : UnitTestBase {
        public SavableEntryTester([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void TestCreationSavingAndKeyCheck()
        {
            SqlConnectionPreparer ms = new SqlConnectionPreparer(Config);
            var db = ms.GetDatabaseConnection(Stage.Testing, Constants.PresentSlice);
            Profile p = new Profile("myProfile", new List<double>().AsReadOnly(), EnergyOrPower.Power);
            SaveableEntry<Profile> sa = SaveableEntry<Profile>.GetSaveableEntry(db, SaveableEntryTableType.PVGeneration, Logger);
            sa.MakeCleanTableForListOfFields(true);
            sa.MakeTableForListOfFieldsIfNotExists(true);
            string myKey = p.Name;
            if (sa.CheckForName(myKey, Logger)) {
                throw new FlaException("Key already exists in cleared db");
            }

            sa.AddRow(p);
            sa.SaveDictionaryToDatabase(Logger);
            if (!sa.CheckForName(myKey, Logger)) {
                throw new FlaException("Saving failed. Key not in db");
            }
        }

        [Fact]
        public void TestProfileCreation()
        {
            SqlConnectionPreparer ms = new SqlConnectionPreparer(Config);
            var dbHouses = ms.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);

            var pvEntries = dbHouses.Fetch<PvSystemEntry>();
            if (pvEntries.Count == 0) {
                throw new FlaException("No PVEntries");
            }

            var houseGuids = pvEntries.Select(x => x.HouseGuid).Distinct().Take(10).ToList();
            // ReSharper disable AssignNullToNotNullAttribute
            ServiceRepository sp = new ServiceRepository(null, null, Logger, Config, new Random());
            // ReSharper restore AssignNullToNotNullAttribute

            // ReSharper disable AssignNullToNotNullAttribute
            var dbdto = new DBDto(null, dbHouses.Fetch<Hausanschluss>(), null, null, null);
            // ReSharper restore AssignNullToNotNullAttribute

            PVProfileProvider pvp = new PVProfileProvider(sp, Constants.PresentSlice, dbdto);
            var hcrc = new HouseComponentRo("name", "type", 1, 1, "status", "", "standort", 0);
            foreach (string houseGuid in houseGuids) {
                var pse = pvEntries.Single(x => x.HouseGuid == houseGuid);
                ProviderParameterDto parameters = new ProviderParameterDto(pse, "dummydir", hcrc);
                pvp.PrepareLoadProfileIfNeeded(parameters);
            }

            foreach (string houseGuid in houseGuids) {
                var pse = pvEntries.Single(x => x.HouseGuid == houseGuid);
                ProviderParameterDto parameters = new ProviderParameterDto(pse, "dummydir", hcrc);
                var result = pvp.ProvideProfile(parameters);
                Info("Got a profile with " + result?.Profile?.EnergySum());
            }
        }
    }
}