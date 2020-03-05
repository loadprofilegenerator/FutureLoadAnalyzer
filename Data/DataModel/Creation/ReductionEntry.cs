using System;

namespace Data.DataModel.Creation {
    public class ReductionEntry {
        [Obsolete("Only for jsons")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public ReductionEntry()
        {
        }

        public ReductionEntry([JetBrains.Annotations.NotNull] string name, double value)
        {
            Name = name;
            Value = value;
        }

        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }

        public double Value { get; set; }
    }
}