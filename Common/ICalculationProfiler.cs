using JetBrains.Annotations;

namespace Common {
    public interface ICalculationProfiler {
        void StartPart([NotNull] string key);

        void StopPart([NotNull] string key);
    }
}