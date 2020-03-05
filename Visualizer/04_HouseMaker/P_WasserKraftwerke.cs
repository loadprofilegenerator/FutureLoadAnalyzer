using BurgdorfStatistics.Tooling;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class P_WasserKraftwerke : RunableWithBenchmark {
        public P_WasserKraftwerke([NotNull] ServiceRepository services)
            : base(nameof(P_WasserKraftwerke), Stage.Houses, 1500, services, false)
        {
            DevelopmentStatus.Add("//Todo: wkw id aus trafokreis");
        }

        protected override void RunActualProcess()
        {
        }
    }
}