using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {

    // ReSharper disable once InconsistentNaming
    public class B07_SelectedHouseExporter : RunableForSingleSliceWithBenchmark {
        public B07_SelectedHouseExporter([NotNull] ServiceRepository services) : base(nameof(B07_SelectedHouseExporter), Stage.ProfileAnalysis, 207, services, false)
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
        }
        protected override void RunChartMaking(ScenarioSliceParameters slice)
        {
            var dbProfileExport = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.HouseProfiles);
            var sa = SaveableEntry<Prosumer>.GetSaveableEntry(dbProfileExport, SaveableEntryTableType.HouseLoad, Services.Logger);
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouses.Fetch<House>();
            var house = houses.Single(x => x.ComplexName == "Kornhausgasse 16");
            List<Profile> relevantProsumers = new List<Profile>();
            foreach (var prosumer in sa.ReadEntireTableDBAsEnumerable()) {
                if (prosumer.HouseGuid == house.Guid) {
                    if (prosumer.Profile == null) {
                        throw new FlaException("profile was null");
                    }
                    relevantProsumers.Add(new Profile(prosumer.Name + " " + prosumer.ProfileSourceName, prosumer.Profile.Values,prosumer.Profile.EnergyOrPower));
                }
            }

            var fn = MakeAndRegisterFullFilename("ExportedProfiles.xlsx", slice);
            XlsxDumper.DumpProfilesToExcel(fn,slice.DstYear, 15,new ProfileWorksheetContent("kornhausgasse 16","Last", relevantProsumers));
        }
    }
}