using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Automation;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FluentAssertions;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using MessagePack.Resolvers;
using Xunit;
using Xunit.Abstractions;
using Gender = Automation.Gender;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class CachingLPGProfileLoaderTest : UnitTestBase {
        [Fact]
        public void RunTest()
        {
            CompositeResolver.RegisterAndSetAsDefault(NativeDateTimeResolver.Instance, StandardResolver.Instance);
            PrepareUnitTest();
            Config.Directories.ResultStorageDirectory = WorkingDirectory.Dir;
            Config.Directories.CalcServerLpgDirectory = WorkingDirectory.Dir;

            // ReSharper disable twice AssignNullToNotNullAttribute

            HouseCreationAndCalculationJob hcj = new HouseCreationAndCalculationJob(Scenario.Present().ToString(), "2017", "trafokreis");
            HouseData hd = new HouseData("houseguid", "HT01", 1000, 1000, "houseName");
            HouseholdData hhd = new HouseholdData("householdguid",
                2000,
                ElectricCarUse.UseElectricCar,
                "householdname",
                ElectricCarProvider.ChargingStationSet,
                ElectricCarProvider.TransportationDevicesOneCar,
                ElectricCarProvider.TravelRouteSet,
                new List<TransportationDistanceModifier>(),
                HouseholdDataSpecifictionType.ByPersons);
            hd.Households.Add(hhd);
            hhd.UseElectricCar = ElectricCarUse.UseElectricCar;
            hhd.TransportationDeviceSet = ElectricCarProvider.TransportationDevicesOneCar;
            hhd.TravelRouteSet = ElectricCarProvider.TravelRouteSet;
            hhd.ChargingStationSet = ElectricCarProvider.ChargingStationSet;

            hhd.HouseholdDataPersonSpecification = new HouseholdDataPersonSpecification(new List<PersonData> {new PersonData(30, Gender.Male)});
            hcj.House = hd;

            List<HouseCreationAndCalculationJob> houseJobs = new List<HouseCreationAndCalculationJob>();
            houseJobs.Add(hcj);
            FileHelpers.CopyRec(Config.Directories.LPGReleaseDirectory, WorkingDirectory.Dir, Logger, true);
            var endTime = new DateTime(Constants.PresentSlice.DstYear, 1, 10);
            ProfileGenerationRo pgro = new ProfileGenerationRo();
            HouseProcessor.WriteDistrictsForLPG(houseJobs,
                WorkingDirectory.DirDi,
                Logger,
                Constants.PresentSlice,
                endTime,
                pgro);
            string districtsDir = WorkingDirectory.Combine("Districts");
            var districtsDi = new DirectoryInfo(districtsDir);
            var files = districtsDi.GetFiles("*.json");

            void RunOneFile(FileInfo myfi)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = WorkingDirectory.Combine("simulationengine.exe");
                psi.UseShellExecute = true;
                psi.WorkingDirectory = WorkingDirectory.Dir;
                psi.Arguments = "ProcessHouseJob  -j \"" + myfi.FullName + "\"";
                Info("running " + psi.FileName + " " + psi.Arguments);
                using (Process p = new Process()) {
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();
                }
            }

            foreach (var housejob in files) {
                RunOneFile(housejob);
            }

            DBDto dbDto = new DBDto(new List<House>(), new List<Hausanschluss>(), new List<Car>(), new List<Household>(), new List<RlmProfile>());
            CachingLPGProfileLoader ca = new CachingLPGProfileLoader(Logger, dbDto);
            List<int> isns = new List<int>();
            isns.Add(10);
            CarDistanceEntry cde = new CarDistanceEntry("houseguid",
                "householdguid",
                "carguid",
                20,
                20,
                isns,
                10,
                "haguid",
                "sourceguid",
                "cdename",
                CarType.Electric);
            HouseComponentRo hcro = new HouseComponentRo("housecomponent", "componeenttype", 1000, 200, "processingstatus", "isns", "standort",0);
            ProviderParameterDto ppd = new ProviderParameterDto(cde, WorkingDirectory.Dir, hcro);
            SqlConnectionPreparer scp = new SqlConnectionPreparer(Config);
            MyDb db = scp.GetDatabaseConnection(Stage.Testing, Constants.PresentSlice);
            SaveableEntry<Profile> sa = SaveableEntry<Profile>.GetSaveableEntry(db, SaveableEntryTableType.LPGProfile, Logger);
            sa.MakeTableForListOfFieldsIfNotExists(true);
            string dstDir = Path.Combine(WorkingDirectory.Dir, hcj.Trafokreis, hcj.House.Name);
            FileHelpers.CopyRec(WorkingDirectory.Combine("Results"), dstDir, Logger, true);

            //normal electricity test and cache test
            Info("================== ");
            Info("electricity");
            Info("================== ");
            var profElec1 = ca.LoadLPGProfile(ppd,
                hcj.Trafokreis,
                "Electricity",
                sa,
                hhd.HouseholdGuid,
                out var profsource,
                hcj.House.Name,
                Config,
                true);
            Info("Source: " + profsource);
            Assert.NotNull(profElec1);
            Assert.NotNull(profsource);

            var profElecCache = ca.LoadLPGProfile(ppd,
                hcj.Trafokreis,
                "Electricity",
                sa,
                hhd.HouseholdGuid,
                out var profsourceCache,
                hcj.House.Name,
                Config,
                true);
            Info("Source 2: " + profsourceCache);
            Assert.NotNull(profsourceCache);
            Assert.NotNull(profsource);
            profElec1.Should().BeEquivalentTo(profElecCache, options => options.Excluding(ctx => ctx.SelectedMemberPath.EndsWith("BinaryProfile")));


            //Car Charging Electricity electricity test and cache test
            Info("================== ");
            Info("Car Charging Electricity electricity");
            Info("================== ");
            SaveableEntry<Profile> sa2 = SaveableEntry<Profile>.GetSaveableEntry(db, SaveableEntryTableType.EvProfile, Logger);
            sa2.MakeCleanTableForListOfFields(true);
            var prof2 = ca.LoadLPGProfile(ppd,
                hcj.Trafokreis,
                "Car Charging Electricity",
                sa2,
                hhd.HouseholdGuid,
                out var profsource2,
                hcj.House.Name,
                Config,
                true);
            Info("Source Wp 1: " + profsource2);
            Assert.NotNull(prof2);
            Assert.NotNull(profsource2);

            var prof3 = ca.LoadLPGProfile(ppd,
                hcj.Trafokreis,
                "Car Charging Electricity",
                sa2,
                hhd.HouseholdGuid,
                out var profsource3,
                hcj.House.Name,
                Config,
                true);
            Info("Source Wp 2: " + profsource3);
            Assert.NotNull(prof3);
            Assert.NotNull(profsource3);

            prof2.Should().BeEquivalentTo(prof3, options => options.Excluding(ctx =>
                ctx.SelectedMemberPath.EndsWith("BinaryProfile")));
        }


        public CachingLPGProfileLoaderTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}