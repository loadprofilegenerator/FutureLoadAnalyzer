using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class BusinessProfileOverrideEntry {
        public BusinessProfileOverrideEntry([NotNull] string houseName,
                                            [NotNull] string businessName,
                                            [NotNull] string standort,
                                            [NotNull] string profileName)
        {
            HouseName = houseName;
            BusinessName = businessName;
            Standort = standort;
            ProfileName = profileName;
        }

        [NotNull]
        public string BusinessName { get; set; }

        [NotNull]
        public string HouseName { get; set; }

        [NotNull]
        public string ProfileName { get; set; }

        [NotNull]
        public string Standort { get; set; }
    }
}