using System.Diagnostics.CodeAnalysis;
using BurgdorfStatistics.Tooling;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Q_BuildingInfrastructureMaker : RunableWithBenchmark {
        public Q_BuildingInfrastructureMaker([NotNull] ServiceRepository services)
            : base(nameof(Q_BuildingInfrastructureMaker), Stage.Houses, 1600, services, false)
        {
            DevelopmentStatus.Add("not implemented");
        }

        protected override void RunActualProcess()
        {
        }
    }
}