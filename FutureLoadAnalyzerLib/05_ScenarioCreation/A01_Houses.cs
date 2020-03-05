using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.Visualisation.SingleSlice;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    [UsedImplicitly]
    public class A01_Houses : RunableForSingleSliceWithBenchmark {
        public A01_Houses([NotNull] ServiceRepository services) : base("A01_Houses",
            Stage.ScenarioCreation,
            100,
            services,
            false,
            new HouseCharts(services, Stage.ScenarioCreation))
        {
            DevelopmentStatus.Add("Destroy houses");
            DevelopmentStatus.Add("Build new Houses");
            DevelopmentStatus.Add("add households to existing houses");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            Info("running house copying");
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<House>();
            dbDstHouses.RecreateTable<Hausanschluss>();
            var srcHouses = dbSrcHouses.Fetch<House>();
            var srcHausanschlusses = dbSrcHouses.Fetch<Hausanschluss>();
            if (srcHouses.Count == 0) {
                throw new FlaException("No houses were found in source slice " + slice.PreviousSlice);
            }

            dbDstHouses.BeginTransaction();
            int housecount = 0;
            foreach (var srcHouse in srcHouses) {
                srcHouse.ID = 0;
                dbDstHouses.Save(srcHouse);
                housecount++;
            }

            int hausanschlusse = 0;
            foreach (var srcha in srcHausanschlusses) {
                srcha.ID = 0;
                hausanschlusse++;
                dbDstHouses.Save(srcha);
            }

            Info("Transfered " + housecount + " houses from " + slice.PreviousSliceNotNull + " to " + slice + " in the scenario " +
                 slice.DstScenario);
            Info("Transfered " + hausanschlusse + " hausanschlusse from " + slice.PreviousSliceNotNull + " to " + slice + " in the scenario " +
                 slice.DstScenario);
            Info("finished house copying");
            dbDstHouses.CompleteTransaction();
        }
    }
}