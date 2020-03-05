using System.Diagnostics.CodeAnalysis;
using Common.Database;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
    [TableName("Cars")]
    [Table("Cars")]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Car : IGuidProvider {
        // ReSharper disable once NotNullMemberIsNotInitialized
        public Car()
        {
        }

        // ReSharper disable once NotNullMemberIsNotInitialized
        public Car([JetBrains.Annotations.NotNull] string carGuid, int age)
        {
            Guid = carGuid;
            Age = age;
            CarType = CarType.Undetermined;
        }

        public Car([JetBrains.Annotations.NotNull] string householdGuid,
                   [JetBrains.Annotations.NotNull] string carGuid,
                   int age,
                   CarType carType,
                   [JetBrains.Annotations.NotNull] string houseGuid)
        {
            HouseholdGuid = householdGuid;
            Guid = carGuid;
            Age = age;
            CarType = carType;
            HouseGuid = houseGuid;
        }

        public int Age { get; set; }

        public CarType CarType { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseholdGuid { get; set; }

        public CarProfileRequirement RequiresProfile { get; set; } = CarProfileRequirement.WithProfile;


        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }
    }

    public enum CarProfileRequirement {
        WithProfile,
        NoProfile
    }
}