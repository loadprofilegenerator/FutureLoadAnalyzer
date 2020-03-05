using JetBrains.Annotations;

namespace Common.Database {
#pragma warning disable CA1040 // Avoid empty interfaces

    public interface IGuidProvider {
        [NotNull]
        string Guid { get; }

        int ID { get; set; }
    }
}