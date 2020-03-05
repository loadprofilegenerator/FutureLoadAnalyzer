using System.Collections.Generic;
using System.IO;
using Automation.ResultFiles;
using Common;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class HouseholdKeyEntryList {
        [NotNull]
        [ItemNotNull]
        public List<HouseholdKeyEntry> HouseholdKeyEntries { get; set; } = new List<HouseholdKeyEntry>();

        private HouseholdKeyEntryList()
        {
        }

        [NotNull]
        public static HouseholdKeyEntryList Load([NotNull] string path)
        {
            LPGReader lpgReader = new LPGReader();
            string resultFile = Path.Combine(path, "Results.General.sqlite");
            HouseholdKeyEntryList hkl = new HouseholdKeyEntryList {HouseholdKeyEntries = lpgReader.ReadFromJson<HouseholdKeyEntry>("HouseholdKeys", resultFile)};
            return hkl;
        }

        [NotNull]
        public HouseholdKeyEntry FindHouseholdKeyByFlaHouseholdKey([NotNull] string flaHouseholdKey, [NotNull] string housename, [NotNull] string loadtypeToSearchFor)
        {
            if (HouseholdKeyEntries.Count == 0) {
                throw new FlaException("not a single household key was loaded");
            }
            foreach (var hhKeyEntry in HouseholdKeyEntries) {
                if (hhKeyEntry.HouseholdDescription.Contains(flaHouseholdKey)) {
                    return hhKeyEntry;
                }
            }

            throw new FlaException( housename + "/" + loadtypeToSearchFor + ": No entry found for householdkey " + flaHouseholdKey + ", " + HouseholdKeyEntries.Count + " other entries exist"   ); //+ "\nkeys:" + hhkeys
        }
    }
}