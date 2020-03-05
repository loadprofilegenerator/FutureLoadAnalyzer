using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B08_ReportLoadProfileExporter : RunableForSingleSliceWithBenchmark {
        public B08_ReportLoadProfileExporter([NotNull] ServiceRepository services) : base(nameof(B08_ReportLoadProfileExporter),
            Stage.ProfileAnalysis,
            208,
            services,
            false)
        {
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var fn = MakeAndRegisterFullFilename(FilenameHelpers.CleanFileName("SummedLoadProfileExport.xlsx"), slice);
            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var bkwArr = dbRaw.Fetch<BkwProfile>();
            var bkwjSonProfile = bkwArr[0];
            var entries = saHouses.LoadAllOrMatching();
            var providerentries = entries.Where(x => x.Key.SumType == SumType.ByProvider).ToList();
            List<Profile> profiles = new List<Profile>();
            foreach (var providerentry in providerentries) {
                providerentry.Profile.Name = (providerentry.Key.ProviderType ?? throw new FlaException()) + " " + providerentry.Key.GenerationOrLoad;
                if (providerentry.Key.GenerationOrLoad == GenerationOrLoad.Load) {
                    profiles.Add(providerentry.Profile);
                }
                else {
                    profiles.Add(providerentry.Profile.MultiplyWith(-1, providerentry.Profile.Name));
                }
            }

            profiles = MergeProfiles(profiles);
            var bkwProfile = new Profile(bkwjSonProfile.Profile);
            bkwProfile.Name = "Messung 2017 [kW]";
            profiles.Add(bkwProfile);
            XlsxDumper.DumpProfilesToExcel(fn, slice.DstYear, 15, new ProfileWorksheetContent("Profiles","Last [kW]", bkwProfile.Name, profiles));
            SaveToArchiveDirectory(fn, RelativeDirectory.Report, slice);
            SaveToPublicationDirectory(fn,slice,"4.4");
            SaveToPublicationDirectory(fn, slice, "5");
            Info("saved " + fn);
        }

        [NotNull]
        [ItemNotNull]
        public static List<Profile> MergeProfiles([NotNull] [ItemNotNull] List<Profile> profiles)
        {
            Dictionary<string, Profile> mergedProfiles = new Dictionary<string, Profile>();
            foreach (var profile in profiles) {
                string newName = ChartHelpers.GetFriendlyProviderName(profile.Name);
                if (!mergedProfiles.ContainsKey(newName)) {
                    profile.Name = newName;
                    mergedProfiles.Add(newName,profile);
                }
                else {
                    mergedProfiles[newName] = mergedProfiles[newName].Add(profile, newName);
                }
            }
            return mergedProfiles.Values.ToList();
        }

    }
}