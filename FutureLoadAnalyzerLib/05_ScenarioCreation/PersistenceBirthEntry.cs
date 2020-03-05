using System;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Creation;
using SQLite;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    public class PersistenceBirthEntry {
        public PersistenceBirthEntry([JetBrains.Annotations.NotNull] string householdKey, Gender gender)
        {
            HouseholdKey = householdKey;
            Gender = gender;
        }

        [Obsolete("only json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public PersistenceBirthEntry()
        {
        }

        public Gender Gender { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }

        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }
    }
}