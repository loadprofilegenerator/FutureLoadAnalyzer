using BurgdorfStatistics.Tooling;
using Common.Steps;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class F_TemperatureProfileImport : RunableWithBenchmark {
        public F_TemperatureProfileImport([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(F_TemperatureProfileImport), Stage.ProfileImport, 600, services, false)
        {
            DevelopmentStatus.Add("Not implemented");
        }

        protected override void RunActualProcess()
        {
        }
    }
}