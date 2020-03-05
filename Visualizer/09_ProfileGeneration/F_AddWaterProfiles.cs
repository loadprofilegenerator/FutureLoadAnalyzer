using BurgdorfStatistics.Tooling;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics._09_ProfileGeneration {
    /// <summary>
    /// make the water profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class F_AddWaterProfiles : RunableForSingleSliceWithBenchmark {
        public F_AddWaterProfiles([NotNull] ServiceRepository services)
            : base(nameof(F_AddWaterProfiles), Stage.ProfileGeneration, 600, services, false)
        {
            DevelopmentStatus.Add("Implementation missing");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
        }
    }
}