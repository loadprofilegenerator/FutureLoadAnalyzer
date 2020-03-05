using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class C06_TemperatureProfileImportTodo : RunableWithBenchmark {
        public C06_TemperatureProfileImportTodo([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(C06_TemperatureProfileImportTodo), Stage.Raw, 206, services, false)
        {
            DevelopmentStatus.Add("Not implemented");
        }

        protected override void RunActualProcess()
        {
        }
    }
}