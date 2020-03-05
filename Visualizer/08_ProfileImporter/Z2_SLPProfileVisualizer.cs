using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace BurgdorfStatistics._08_ProfileImporter {
    // this class is just for testing the slp provider and visualizing the results
    // ReSharper disable once InconsistentNaming
    public class Z2_SLPProfileVisualizer : RunableWithBenchmark {
        public Z2_SLPProfileVisualizer([NotNull] ServiceRepository services)
            : base(nameof(Z2_SLPProfileVisualizer), Stage.ProfileImport, 2620, services, false)
        {
        }

        protected override void RunActualProcess()
        {
        }

        protected override void RunChartMaking()
        {
            var dbImport = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice);
            var slpValues = dbImport.Database.Fetch<VDEWProfileValues>();
            var sp = new SLPProvider(2017);
            var pH0 = sp.Run(slpValues, "H0", 1000);
            var filename = MakeAndRegisterFullFilename("H0.png", "SLPTests", "SlpTests", Constants.PresentSlice);
            Services.PlotMaker.MakeLineChart(filename, "Leistung", pH0);

            var pG0 = sp.Run(slpValues, "G0", 1000);
            filename = MakeAndRegisterFullFilename("G0.png", "SLPTests", "SlpTests", Constants.PresentSlice);
            Services.PlotMaker.MakeLineChart(filename, "Leistung", pG0);


            var pG1 = sp.Run(slpValues, "G1", 1000);
            filename = MakeAndRegisterFullFilename("G1.png", "SLPTests", "SlpTests", Constants.PresentSlice);
            Services.PlotMaker.MakeLineChart(filename, "Leistung", pG1);
        }
    }
}