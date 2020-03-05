using System;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class A05_Kleinwasserkraft : RunableForSingleSliceWithBenchmark {
        public A05_Kleinwasserkraft([NotNull] ServiceRepository services)
            : base(nameof(A05_Kleinwasserkraft), Stage.ScenarioCreation, 105,
                services, false)
        {
            //todo: include factors for adjusting the energy consumption into the parameters
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            if (slice.PreviousSlice == null) {
                throw new FlaException("Previous slice was null");
            }
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSlice);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<KleinWasserkraft>();
            var srcKwk = dbSrcHouses.Fetch<KleinWasserkraft>();
            if (srcKwk.Count == 0) {
                throw new Exception("No building infrastructures were found");
            }
            dbDstHouses.BeginTransaction();
            foreach (var wasserkraft in srcKwk) {
                wasserkraft.ID = 0;
                dbDstHouses.Save(wasserkraft);
            }

            dbDstHouses.CompleteTransaction();
        }
    }
}