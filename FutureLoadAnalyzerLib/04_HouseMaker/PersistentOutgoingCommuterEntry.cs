using System;
using Data.DataModel.Creation;
using SQLite;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class PersistentOutgoingCommuterEntry {
        public PersistentOutgoingCommuterEntry([JetBrains.Annotations.NotNull] string householdKey,
                                               double distanceInKm,
                                               CommuntingMethod communtingMethod,
                                               [JetBrains.Annotations.NotNull] string workCity,
                                               [JetBrains.Annotations.NotNull] string workKanton)
        {
            HouseholdKey = householdKey;
            DistanceInKm = distanceInKm;
            CommuntingMethod = communtingMethod;
            WorkCity = workCity;
            WorkKanton = workKanton;
        }

        [Obsolete("json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public PersistentOutgoingCommuterEntry()
        {
        }

        public CommuntingMethod CommuntingMethod { get; set; }

        public double DistanceInKm { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }

        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string WorkCity { get; set; }

        [JetBrains.Annotations.NotNull]
        public string WorkKanton { get; set; }
    }
}