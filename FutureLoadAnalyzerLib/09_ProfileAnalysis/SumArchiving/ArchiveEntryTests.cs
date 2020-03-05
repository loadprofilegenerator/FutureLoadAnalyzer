using System;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving {
    public class ArchiveEntryTests : UnitTestBase {
        [Fact]
        public void TestArchiveEntry()
        {
            Profile p = Profile.MakeConstantProfile(10000,"profilename",Profile.ProfileResolution.QuarterHour);
            ArchiveEntry ae = new ArchiveEntry("name",new AnalysisKey("tk","pt",SumType.ByTrafokreisAndProvider,
                GenerationOrLoad.Generation, "housename",
                "profilesource", "household"), p,GenerationOrLoad.Load,"trafokreis");
            // ReSharper disable twice AssignNullToNotNullAttribute
            ServiceRepository services = new ServiceRepository(null,null,Logger,Config,new Random());
            var db = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Testing, Constants.PresentSlice);
            var sa = SaveableEntry<ArchiveEntry>.GetSaveableEntry(db, SaveableEntryTableType.HouseLoad, Logger);
            sa.MakeCleanTableForListOfFields(false);
            sa.AddRow(ae);
            sa.SaveDictionaryToDatabase(Logger);
            var loadedae = sa.LoadAllOrMatching();
            loadedae[0].Should().BeEquivalentTo(ae);
        }

        public ArchiveEntryTests([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}