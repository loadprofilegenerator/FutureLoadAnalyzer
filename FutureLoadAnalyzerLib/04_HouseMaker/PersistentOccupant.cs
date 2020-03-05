using Data.DataModel.Creation;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class PersistentOccupant {
        public PersistentOccupant(int age, Gender gender)
        {
            Age = age;
            Gender = gender;
        }

        public PersistentOccupant()
        {
        }

        public int Age { get; set; }

        public Gender Gender { get; set; }
    }
}