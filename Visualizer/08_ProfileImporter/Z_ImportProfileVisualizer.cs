using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Visualizer;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class Z_ImportProfileVisualizer : RunableWithBenchmark {
        public Z_ImportProfileVisualizer([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(Z_ImportProfileVisualizer), Stage.ProfileImport, 2600, services, false)
        {
        }

        protected override void RunChartMaking()
        {
            ScenarioSliceParameters slice = Constants.PresentSlice;
            double min = 0;
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            {
                var allLs = new List<LineSeriesEntry>();
                var bkws = dbSrcProfiles.Fetch<BkwProfile>();
                var bkw = bkws[0];
                var ls = bkw.Profile.GetLineSeriesEntry();
                allLs.Add(ls);
                var filename = MakeAndRegisterFullFilename("BKW_Profile.png", Name, "", slice);
                Services.PlotMaker.MakeLineChart(filename, bkw.Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }

            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            foreach (var rlm in rlms) {
                var allLs = new List<LineSeriesEntry>();

                var ls1 = rlm.Profile.GetLineSeriesEntry();
                allLs.Add(ls1);

                var filename = MakeAndRegisterFullFilename("RLMProfile." + rlm.Name + ".png", Name, "", slice);
                min = Math.Min(0, rlm.Profile.Values.Min());
                Services.PlotMaker.MakeLineChart(filename, rlm.Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }
        }

        protected override void RunActualProcess()
        {
        }
    }
}