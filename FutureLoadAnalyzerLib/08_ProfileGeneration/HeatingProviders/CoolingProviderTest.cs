using System;
using System.Collections.Generic;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingProviderTest : UnitTestBase {
        public CoolingProviderTest([CanBeNull] ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void Run()
        {
            PrepareUnitTest();

            // ReSharper disable AssignNullToNotNullAttribute
            ServiceRepository sp = new ServiceRepository(null, null, Logger, Config, new Random());

            // ReSharper restore AssignNullToNotNullAttribute
            var slice = Constants.PresentSlice;
            var dbHouse = sp.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var airconditioningEntries = dbHouse.Fetch<AirConditioningEntry>();
            var hausanschlusses = dbHouse.Fetch<Hausanschluss>();
            DBDto dbDto = new DBDto(new List<House>(), hausanschlusses, new List<Car>(), new List<Household>(), new List<RlmProfile>());
            CoolingProvider hp = new CoolingProvider(sp, slice, dbDto);
            Info("total hse: " + airconditioningEntries.Count);
            Profile sumProfile = Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour);
            foreach (var entry in airconditioningEntries) {
                (entry.HouseComponentType == HouseComponentType.Cooling).Should().BeTrue();
                hp.IsCorrectProvider(entry).Should().BeTrue();
                HouseComponentRo hro = new HouseComponentRo(string.Empty, string.Empty, 1, 1, string.Empty, string.Empty, string.Empty, 0);
                ProviderParameterDto ppd = new ProviderParameterDto(entry, string.Empty, hro);
                hp.PrepareLoadProfileIfNeeded(ppd);
                var prof = hp.ProvideProfile(ppd);
                if (prof != null) {
                    if (prof.Profile == null) {
                        throw new FlaException("Profile was null");
                    }

                    prof.Profile.EnergySum().Should().BeApproximately(entry.EffectiveEnergyDemand, entry.EffectiveEnergyDemand * 0.1);
                    sumProfile = sumProfile.Add(prof.Profile.Values);
                }
            }

            string fn = WorkingDirectory.Combine("Profiletest_cooling.xlsx");
            XlsxDumper.DumpProfilesToExcel(fn, 2050, 15, new ProfileWorksheetContent("Sum", "Last", 240, sumProfile));
            Info("Wrote to " + fn);
        }
    }
}