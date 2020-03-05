using System;
using Data.DataModel.Creation;
using SQLite;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class PersistentCar {
        public PersistentCar(CarType carType, [JetBrains.Annotations.NotNull] string householdKey, [JetBrains.Annotations.NotNull] string houseName, int age)
        {
            CarType = carType;
            HouseholdKey = householdKey;
            HouseName = houseName;
            Age = age;
        }
        [Obsolete("json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public PersistentCar()
        {
        }
        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }
        public CarType CarType { get; set; }
        [JetBrains.Annotations.NotNull]
        public string HouseholdKey { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseName { get; set; }
        public int Age { get; set; }

    }
}