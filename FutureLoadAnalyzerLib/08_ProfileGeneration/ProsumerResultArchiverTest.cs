using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    public class ProsumerResultArchiverTest: UnitTestBase {
        [Fact]
        public void Run()
        {
            Random rnd = new Random();
            // ReSharper disable twice AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null,null,Logger,Config,rnd);
            ProsumerComponentResultArchiver pra = new ProsumerComponentResultArchiver(Stage.Testing,Constants.PresentSlice,HouseProcessor.ProcessingMode.Collecting,
                services);
            Prosumer prosumer = new Prosumer("houseguid","housename",HouseComponentType.Household,"sourceguid",1,
                "hausanschlussguid","haussanschlusskey",GenerationOrLoad.Load, "trafokreis","providername", "profileSource");
            Stopwatch sw = Stopwatch.StartNew();
            var createdProsumers = new List<Prosumer>();
            for (int i = 0; i < 200; i++) {
                Profile prof = Profile.MakeRandomProfile(rnd, "name", Profile.ProfileResolution.QuarterHour);
                prosumer.Profile = prof;
                pra.Archive(prosumer);
                createdProsumers.Add(prosumer);
            }
            Info("Creating and queueing: " + sw.Elapsed.ToString());
            pra.FinishSavingEverything();
            pra.Dispose();
            sw.Stop();
            Info("total: " + sw.Elapsed);
            var loadSa = SaveableEntry<Prosumer>.GetSaveableEntry(services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Testing, Constants.PresentSlice, DatabaseCode.HouseProfiles),
                SaveableEntryTableType.HouseLoad, Logger);
            var loadedProsumers = loadSa.LoadAllOrMatching();
            Info("checking profile counts");
            foreach (var loadedProsumer in loadedProsumers) {

                loadedProsumer.Profile?.Values.Count.Should().Be(8760*4);
            }
            Info("profile counts were ok, checking prosumers");
            sw = Stopwatch.StartNew();
            for (int i = 0; i < createdProsumers.Count && i < 1; i++) {
                loadedProsumers[i].Should().BeEquivalentTo(createdProsumers[i]);
            }
            sw.Stop();
            Info("checking prosumers took " + sw.Elapsed.ToString());
        }


        public ProsumerResultArchiverTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}