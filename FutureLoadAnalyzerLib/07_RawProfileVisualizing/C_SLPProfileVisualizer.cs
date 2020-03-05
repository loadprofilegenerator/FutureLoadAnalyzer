using Common;
using Common.Steps;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._07_RawProfileVisualizing {
    // this class is just for testing the slp provider and visualizing the results
    // ReSharper disable once InconsistentNaming
    public class C_SLPProfileVisualizer : RunableWithBenchmark {
        public C_SLPProfileVisualizer([NotNull] ServiceRepository services)
            : base(nameof(C_SLPProfileVisualizer), Stage.RawProfileVisualisation, 300, services, false)
        {
        }

        protected override void RunActualProcess()
        {
        }

        protected override void RunChartMaking()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var slpValues = dbRaw.Fetch<VDEWProfileValue>();
            var feiertage = dbRaw.Fetch<FeiertagImport>();
            var sp = new SLPProvider(2017,slpValues, feiertage);
            var pH0 = sp.Run( "H0", 1000);
            var filename = MakeAndRegisterFullFilename("H0.png", Constants.PresentSlice);
            Services.PlotMaker.MakeLineChart(filename, "Leistung", pH0);

            var pG0 = sp.Run( "G0", 1000);
            filename = MakeAndRegisterFullFilename("G0.png", Constants.PresentSlice);
            Services.PlotMaker.MakeLineChart(filename, "Leistung", pG0);


            var pG1 = sp.Run( "G1", 1000);
            filename = MakeAndRegisterFullFilename("G1.png", Constants.PresentSlice);
            Services.PlotMaker.MakeLineChart(filename, "Leistung", pG1);
        }
    }
}