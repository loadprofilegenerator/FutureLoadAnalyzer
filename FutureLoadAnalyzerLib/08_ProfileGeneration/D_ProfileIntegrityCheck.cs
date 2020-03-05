using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class D_ProfileIntegrityCheck : RunableForSingleSliceWithBenchmark {
        public D_ProfileIntegrityCheck([NotNull] ServiceRepository services) : base(nameof(D_ProfileIntegrityCheck),
            Stage.ProfileGeneration,
            400,
            services,
            false,
            null)
        {
        }

        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            //var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            //todo: do this
            //todo: check profiles for heating profiles in 2017
        }

    }
}