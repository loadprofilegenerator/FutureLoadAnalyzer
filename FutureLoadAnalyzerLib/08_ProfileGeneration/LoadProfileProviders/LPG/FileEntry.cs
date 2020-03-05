using System.IO;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class FileEntry {
        public FileEntry([NotNull] string householdKey, [NotNull] string fullFilename)
        {
            HouseholdKey = householdKey;
            FullFilename = fullFilename;
            FileInfo fi = new FileInfo(FullFilename);
            Filename = fi.Name;
        }

        [NotNull]
        public string HouseholdKey { get; }
        [NotNull]
        public string FullFilename { get; }
        [NotNull]
        public string Filename { get; }
    }
}