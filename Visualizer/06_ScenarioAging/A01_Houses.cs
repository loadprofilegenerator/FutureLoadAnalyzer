using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class A01_Houses : RunableForSingleSliceWithBenchmark {
        public A01_Houses([NotNull] ServiceRepository services)
            : base("A01_Houses", Stage.ScenarioCreation, 100, services, false,new HouseCharts())
        {
            DevelopmentStatus.Add("Destroy houses");
            DevelopmentStatus.Add("Build new Houses");
            DevelopmentStatus.Add("add households to existing houses");
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Log(MessageType.Debug, "running house copying");
            Services.SqlConnection.RecreateTable<House>(Stage.Houses, parameters);
            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenarioNotNull).Database;
            var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var srcHouses = dbSrcHouses.Fetch<House>();
            if (srcHouses.Count == 0) {
                throw new FlaException("No houses were found in source slice " + parameters.PreviousScenario);
            }
            dbDstHouses.BeginTransaction();
            int housecount = 0;
            foreach (var srcHouse in srcHouses) {
                srcHouse.HouseID = 0;
                dbDstHouses.Save(srcHouse);
                housecount++;
            }
            dbDstHouses.CompleteTransaction();
            Log(MessageType.Info,"Transfered " + housecount + " houses from " + parameters.PreviousScenarioNotNull +  " to " + parameters   + " in the scenario " + parameters.DstScenario);
            Log(MessageType.Debug, "finished house copying");
        }
    }
}