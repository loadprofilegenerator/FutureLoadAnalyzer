using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer;

// ReSharper disable once InconsistentNaming
namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    /// <summary>
    ///     calculate the residual load
    /// </summary>
    public class A01_LastgangResidualCalc : RunableForSingleSliceWithBenchmark {
        public A01_LastgangResidualCalc([NotNull] ServiceRepository services) : base(nameof(A01_LastgangResidualCalc),
            Stage.ProfileAnalysis,
            100,
            services,
            false)
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbDstProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice);
            dbDstProfiles.RecreateTable<ResidualProfile>();
            var bkw = dbSrcProfiles.Fetch<BkwProfile>();
            var rlmProfiles = dbSrcProfiles.Fetch<RlmProfile>();
            var main = new Profile(bkw[0].Profile);
            foreach (var rlm in rlmProfiles) {
                Profile profile = new Profile(rlm.Name, rlm.Profile.Values, rlm.Profile.EnergyOrPower);
                main = main.Minus(profile, "residual");
            }

            JsonSerializableProfile jsp = new JsonSerializableProfile(main);
            var residualProfile = new ResidualProfile("Residual after all RLMs") {
                Profile = jsp
            };
            dbDstProfiles.BeginTransaction();
            dbDstProfiles.Save(residualProfile);
            dbDstProfiles.CompleteTransaction();
        }

        protected override void RunChartMaking([NotNull] ScenarioSliceParameters slice)
        {
            double min = 0;
            var dbSrcProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            {
                var allLs = new List<LineSeriesEntry>();
                var bkws = dbSrcProfiles.Fetch<BkwProfile>();
                var bkw = new Profile(bkws[0].Profile);
                var ls = bkw.GetLineSeriesEntry();
                allLs.Add(ls);

                var filename = MakeAndRegisterFullFilename("Profile_BKW.png", slice);

                Services.PlotMaker.MakeLineChart(filename, bkw.Name, allLs, new List<AnnotationEntry>(), min);
            }

            {
                var dbGEneratedProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice);

                var allLs = new List<LineSeriesEntry>();
                var residual = dbGEneratedProfiles.Fetch<ResidualProfile>();
                if (residual[0].Profile == null) {
                    throw new Exception("Profile was null");
                }

                Profile myprofile = new Profile(residual[0].Profile);
                var ls = myprofile.GetLineSeriesEntry();
                allLs.Add(ls);
                min = Math.Min(0, residual[0].Profile.Values.Min());

                var filename = MakeAndRegisterFullFilename("Profile_Residual.png", slice);

                Services.PlotMaker.MakeLineChart(filename, residual[0].Name, allLs, new List<AnnotationEntry>(), min);
            }

            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            foreach (var rlm in rlms) {
                var allLs = new List<LineSeriesEntry>();
                Profile profile = new Profile(rlm.Name, rlm.Profile.Values, rlm.Profile.EnergyOrPower);
                var ls1 = profile.GetLineSeriesEntry();
                allLs.Add(ls1);

                var filename = MakeAndRegisterFullFilename("Profile." + rlm.Name + ".png", slice);
                min = Math.Min(0, rlm.Profile.Values.Min());
                Services.PlotMaker.MakeLineChart(filename, rlm.Name, allLs, new List<AnnotationEntry>(), min);
            }
        }
    }
}