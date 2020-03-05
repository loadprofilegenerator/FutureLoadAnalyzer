using BurgdorfStatistics.Tooling;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics._09_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class G_AddHeatpumpProfiles : RunableForSingleSliceWithBenchmark {
        public G_AddHeatpumpProfiles([NotNull] ServiceRepository services)
            : base(nameof(G_AddHeatpumpProfiles), Stage.ProfileGeneration, 700, services, false)
        {
            DevelopmentStatus.Add("Implementation missing");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
        }
    }
}