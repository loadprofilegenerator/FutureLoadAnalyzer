using System;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.ProfileImport;

namespace Data.DataModel.Profiles {
    public class VDEWProfileValue {
        [Obsolete("Only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public VDEWProfileValue()
        {
        }

        public VDEWProfileValue([JetBrains.Annotations.NotNull] string profileName, Season season, int minutes, double value, TagTyp tagTyp)
        {
            ProfileName = profileName;
            Season = season;
            Minutes = minutes;
            Value = value;
            TagTyp = tagTyp;
        }

        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ProfileName { get; set; }
        public Season Season { get; set; }
        public int Minutes { get; set; }
        public double Value { get; set; }
        public TagTyp TagTyp { get; set; }
    }
}