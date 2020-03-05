using System;
using System.Collections.Generic;
using System.IO;
using Automation.ResultFiles;
using Common;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class ResultFileEntryLoader {
        private ResultFileEntryLoader()
        {
        }

        [ItemNotNull]
        [NotNull]
        private List<ResultFileEntry> Files { get; } = new List<ResultFileEntry>();
        [NotNull]
        public static ResultFileEntryLoader Load([NotNull] string path)
        {
            LPGReader lpgReader = new LPGReader();
            string resultFile = Path.Combine(path, "Results.General.sqlite");
            var results = lpgReader.ReadFromJson<ResultFileEntry>("ResultFileEntries", resultFile);
            ResultFileEntryLoader rfel = new ResultFileEntryLoader();
            rfel.Files.Clear();
            rfel.Files.AddRange(results);
            return rfel;
        }

        [CanBeNull]
        public ResultFileEntry FindCorrectProfile([NotNull] HouseholdKey key, [NotNull] string loadtype)
        {
            if (Files.Count == 0) {
                throw new FlaException("Not a single file was found");
            }

            foreach (var resultFileEntry in Files) {
                if (resultFileEntry.ResultFileID == ResultFileID.ExternalSumsForHouseholdsJson && resultFileEntry.HouseholdKey == key.Key) {
                    if (String.Equals(resultFileEntry.LoadTypeInformation.Name, loadtype, StringComparison.InvariantCultureIgnoreCase)) {
                        return resultFileEntry;
                    }
                }
            }
            return null;
        }
    }
}