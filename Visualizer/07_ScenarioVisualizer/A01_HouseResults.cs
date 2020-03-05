using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer;

namespace BurgdorfStatistics._07_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    public class A01_HouseResults: RunnableForScenarioWithBenchmark
    {
        //stacked bar chart for each element
        public A01_HouseResults([NotNull] ServiceRepository services)
            : base(nameof(A01_HouseResults), Stage.ScenarioVisualisation, 100, services,false)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices)
        {
            Info("starting to make house results");
            LineSeriesEntry housesCount = new LineSeriesEntry("Häuser");
            LineSeriesEntry householdsCount = new LineSeriesEntry("Haushalte");
            LineSeriesEntry occupantsCount = new LineSeriesEntry("Bewohner");
            foreach (var slice in allSlices) {
                var dbh = SqlConnection.GetDatabaseConnection(Stage.Houses, slice);
                var houses = dbh.Database.Fetch<House>();
                housesCount.Values.Add(new Point(slice.DstYear,houses.Count));

                var households = dbh.Database.Fetch<Household>();
                householdsCount.Values.Add(new Point(slice.DstYear, households.Count));

                var occupants = dbh.Database.Fetch<Occupant>();
                occupantsCount.Values.Add(new Point(slice.DstYear, occupants.Count));
            }

            var s = allSlices.Last();
            var filename1 = MakeAndRegisterFullFilename("HousesForScenario." + s + ".png", Name, "",s);
            Services.PlotMaker.MakeLineChart(filename1,"Anzahl Häuser", housesCount, new List<PlotMaker.AnnotationEntry>());

            var filename2 = MakeAndRegisterFullFilename("HouseholdsForScenario." + s + ".png", Name, "", s);
            Services.PlotMaker.MakeLineChart(filename2, "Anzahl Haushalte", householdsCount, new List<PlotMaker.AnnotationEntry>());

            var filename3 = MakeAndRegisterFullFilename("OccupantForScenario." + s + ".png", Name, "", s);
            Services.PlotMaker.MakeLineChart(filename3, "Anzahl Einwohner", occupantsCount, new List<PlotMaker.AnnotationEntry>());
        }
    }
}