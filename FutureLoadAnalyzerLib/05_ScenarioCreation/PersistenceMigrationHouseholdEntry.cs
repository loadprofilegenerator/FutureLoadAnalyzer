using System;
using System.Diagnostics.CodeAnalysis;
using SQLite;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    public class PersistenceMigrationHouseholdEntry {
        public PersistenceMigrationHouseholdEntry([JetBrains.Annotations.NotNull] string houseName,
                                                  [JetBrains.Annotations.NotNull] string name,
                                                  [JetBrains.Annotations.NotNull] string hausanschlussObjektID)
        {
            HouseName = houseName;
            Name = name;
            HausanschlussObjektID = hausanschlussObjektID;
        }

        [Obsolete("only json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public PersistenceMigrationHouseholdEntry()
        {
        }

        [JetBrains.Annotations.NotNull]
        public string HausanschlussObjektID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseName { get; set; }

        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }
    }
}