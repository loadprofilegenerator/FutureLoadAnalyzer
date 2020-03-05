using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class A03_Businesses : RunableForSingleSliceWithBenchmark {
        public A03_Businesses([NotNull] ServiceRepository services)
            : base(nameof(A03_Businesses), Stage.ScenarioCreation, 103,
                services, false, new BusinessCharts())
        {
            DevelopmentStatus.Add("Add day/night check for ht/nt");
            DevelopmentStatus.Add("adjust profile to other day/night or make a flat profile if needed");
            DevelopmentStatus.Add("Export High voltage");
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
              Services.SqlConnection.RecreateTable<BusinessEntry>(Stage.Houses, parameters);
              var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenarioNotNull).Database;
              var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
              var srcBusinessEntries = dbSrcHouses.Fetch<BusinessEntry>();
              if (srcBusinessEntries.Count == 0)
              {
                  throw new FlaException("No srcBusinessEntries were found");
              }

              foreach (var entry in srcBusinessEntries) {
                  entry.BusinessID = 0;
                  dbDstHouses.Save(entry);
              }
            dbDstHouses.CompleteTransaction();
        }
    }
}