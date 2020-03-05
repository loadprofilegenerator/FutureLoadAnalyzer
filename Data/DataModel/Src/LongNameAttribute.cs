using System;

namespace Data.DataModel.Src {
    [AttributeUsage(AttributeTargets.All)]
    public sealed class LongNameAttribute : Attribute {
        public LongNameAttribute([JetBrains.Annotations.NotNull] string longName) => LongName = longName;

        [JetBrains.Annotations.NotNull]
        public string LongName { get; }
    }
}