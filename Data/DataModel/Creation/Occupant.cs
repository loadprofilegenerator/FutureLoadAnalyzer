using System;
using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Database;
using JetBrains.Annotations;

namespace Data.DataModel.Creation {
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    public class Occupant : IGuidProvider {
        public Occupant([NotNull] string occupantGuid, int age, Gender gender)
        {
            Guid = occupantGuid;
            Age = age;
            Gender = gender;
        }

        [Obsolete("Only for json")]
        public Occupant()
        {
        }

        public Occupant([NotNull] string householdGuid,
                        [NotNull] string occupantGuid,
                        int age,
                        Gender gender,
                        [NotNull] string houseGuid,
                        [NotNull] string householdKey)
        {
            if (householdGuid == null) {
                throw new FlaException("householdguid was null");
            }

            HouseholdGuid = householdGuid;
            Guid = occupantGuid;
            Age = age;
            Gender = gender;
            HouseGuid = houseGuid;
            HouseholdKey = householdKey;
            if (HouseholdKey == null) {
                throw new FlaException("householdkey was null");
            }
        }

        public int Age { get; set; }

        public Gender Gender { get; set; }

        [NotNull]
        public string HouseGuid { get; set; }


        [NotNull]
        public string HouseholdGuid { get; set; }

        [NotNull]
        public string HouseholdKey { get; set; }

        [NotNull]
        public string Guid { get; set; }

        public int ID { get; set; }
    }
}