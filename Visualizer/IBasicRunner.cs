using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer {
    public interface IBasicRunner {
        Stage MyStage { get; }
        int SequenceNumber { get; }
        Steptype StepType { get; }

        [NotNull]
        string Name { get; }
    }
}