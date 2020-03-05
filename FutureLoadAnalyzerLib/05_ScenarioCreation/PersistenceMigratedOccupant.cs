﻿using System;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Creation;
using SQLite;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    public class PersistenceMigratedOccupant {
        public PersistenceMigratedOccupant([JetBrains.Annotations.NotNull] string householdKey, int age, Gender gender)
        {
            HouseholdKey = householdKey;
            Age = age;
            Gender = gender;
        }

        [Obsolete("json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public PersistenceMigratedOccupant()
        {
        }

        public int Age { get; set; }
        public Gender Gender { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }

        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }
    }
}