using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using Visualizer;

namespace FutureLoadAnalyzerLib._07_RawProfileVisualizing {
    // ReSharper disable once InconsistentNaming
    public class B_ImportProfileVisualizer : RunableWithBenchmark {
        public B_ImportProfileVisualizer([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(B_ImportProfileVisualizer), Stage.RawProfileVisualisation, 200, services, false)
        {
        }

        protected override void RunChartMaking()
        {
            ScenarioSliceParameters slice = Constants.PresentSlice;
            double min = 0;
            var dbSrcProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            {
                var allLs = new List<LineSeriesEntry>();
                var bkws = dbSrcProfiles.Fetch<BkwProfile>();
                var bkwProf = new Profile(bkws[0].Profile);
                var ls = bkwProf.GetLineSeriesEntry();
                allLs.Add(ls);
                var filename = MakeAndRegisterFullFilename("BKW_Profile.png", slice);
                Services.PlotMaker.MakeLineChart(filename, bkwProf.Name, allLs, new List<AnnotationEntry>(), min);
            }

            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            foreach (var rlm in rlms) {
                var allLs = new List<LineSeriesEntry>();
                Profile profile = new Profile(rlm.Name,rlm.Profile.Values,rlm.Profile.EnergyOrPower);
                var ls1 = profile.GetLineSeriesEntry();
                allLs.Add(ls1);

                var filename = MakeAndRegisterFullFilename("RLMProfile." + rlm.Name + ".png", slice);
                min = Math.Min(0, rlm.Profile.Values.Min());
                Services.PlotMaker.MakeLineChart(filename, rlm.Name, allLs, new List<AnnotationEntry>(), min);
            }
        }

        protected override void RunActualProcess()
        {
        }
    }
}