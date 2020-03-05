using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class A01_HouseResultsCharts: RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A01_HouseResultsCharts([NotNull] ServiceRepository services)
            : base(nameof(A01_HouseResultsCharts), Stage.ScenarioVisualisation, 100, services,false)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            if (!Services.RunningConfig.MakeCharts) {
                return;
            }
            Info("starting to make house results");
            LineSeriesEntry housesCount = new LineSeriesEntry("Häuser");
            LineSeriesEntry householdsCount = new LineSeriesEntry("Haushalte");
            LineSeriesEntry occupantsCount = new LineSeriesEntry("Bewohner");
            List<ScenarioSliceParameters> missingSlices = new List<ScenarioSliceParameters>();
            foreach (var slice in allSlices) {
                Info("Checking for slice " + slice);
                var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
                var fi = new FileInfo(db.DBFilename);
                if (!fi.Exists) {
                    missingSlices.Add(slice);
                }
            }

            if (missingSlices.Count > 0) {
                var missingSliceNames = missingSlices.Select(x => x.ToString()).ToList();
                string missingSlicesStr = string.Join("\n", missingSliceNames);
                throw new FlaException("Missing Slice Names: " + missingSlicesStr);
            }
            foreach (var slice in allSlices) {
                Info("Reading slice " + slice);
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                housesCount.Values.Add(new Point(slice.DstYear,houses.Count));

                var households = analysisRepo.GetSlice(slice).Fetch<Household>();
                householdsCount.Values.Add(new Point(slice.DstYear, households.Count));

                var occupants = households.SelectMany(x => x.Occupants).ToList();
                occupantsCount.Values.Add(new Point(slice.DstYear, occupants.Count));

            }

            var s = Constants.PresentSlice;
            var filename1 = MakeAndRegisterFullFilename("HousesForScenario." + s + ".png",s);
            Services.PlotMaker.MakeLineChart(filename1,"Anzahl Häuser", housesCount, new List<AnnotationEntry>());

            var filename2 = MakeAndRegisterFullFilename("HouseholdsForScenario." + s + ".png", s);
            Services.PlotMaker.MakeLineChart(filename2, "Anzahl Haushalte", householdsCount, new List<AnnotationEntry>());

            var filename3 = MakeAndRegisterFullFilename("OccupantForScenario." + s + ".png", s);
            Services.PlotMaker.MakeLineChart(filename3, "Anzahl Einwohner", occupantsCount, new List<AnnotationEntry>());
        }
    }
}