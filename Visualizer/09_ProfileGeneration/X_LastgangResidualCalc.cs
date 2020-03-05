using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Visualizer;

// ReSharper disable once InconsistentNaming
namespace BurgdorfStatistics._09_ProfileGeneration {
    /// <summary>
    /// calculate the residual load
    /// </summary>
    public class X_LastgangResidualCalc : RunableForSingleSliceWithBenchmark {
        public X_LastgangResidualCalc([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(X_LastgangResidualCalc), Stage.ProfileGeneration, 2500, services, false)
        {
        }

        protected override void RunChartMaking([JetBrains.Annotations.NotNull] ScenarioSliceParameters parameters)
        {
            double min = 0;
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            {
                var allLs = new List<LineSeriesEntry>();
                var bkws = dbSrcProfiles.Fetch<BkwProfile>();
                var bkw = bkws[0];
                var ls = bkw.Profile.GetLineSeriesEntry();
                allLs.Add(ls);

                var filename = MakeAndRegisterFullFilename("Profile_BKW.png", Name, "", parameters);

                Services.PlotMaker.MakeLineChart(filename, bkw.Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }

            {
                var dbGEneratedProfiles = Services.SqlConnection.GetDatabaseConnection(
                    Stage.ProfileGeneration, parameters).Database;

                var allLs = new List<LineSeriesEntry>();
                var residual = dbGEneratedProfiles.Fetch<ResidualProfile>();
                if (residual[0].Profile == null) {
                    throw new Exception("Profile was null");
                }

                var ls = residual[0].Profile.GetLineSeriesEntry();
                allLs.Add(ls);
                min = Math.Min(0, residual[0].Profile.Values.Min());

                var filename = MakeAndRegisterFullFilename("Profile_Residual.png", Name, "", parameters);

                Services.PlotMaker.MakeLineChart(filename, residual[0].Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }

            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            foreach (var rlm in rlms) {
                var allLs = new List<LineSeriesEntry>();

                var ls1 = rlm.Profile.GetLineSeriesEntry();
                allLs.Add(ls1);

                var filename = MakeAndRegisterFullFilename("Profile." + rlm.Name + ".png", Name, "", parameters);
                min = Math.Min(0, rlm.Profile.Values.Min());
                Services.PlotMaker.MakeLineChart(filename, rlm.Name, allLs, new List<PlotMaker.AnnotationEntry>(), min);
            }
        }

        protected override void RunActualProcess([JetBrains.Annotations.NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<ResidualProfile>(Stage.ProfileGeneration, parameters);
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters).Database;
            var bkw = dbSrcProfiles.Fetch<BkwProfile>();
            var rlmProfiles = dbSrcProfiles.Fetch<RlmProfile>();
            var main = bkw[0].Profile;
            foreach (var rlm in rlmProfiles) {
                main = main.MinusProfile(rlm.Profile, "residual");
            }

            var residualProfile = new ResidualProfile("Residual after all RLMs") {
                Profile = main,
            };
            dbDstProfiles.BeginTransaction();
            dbDstProfiles.Save(residualProfile);
            dbDstProfiles.CompleteTransaction();
        }
    }
}