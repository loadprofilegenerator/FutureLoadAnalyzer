using System;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class A03_Businesses : RunableForSingleSliceWithBenchmark {
        public A03_Businesses([NotNull] ServiceRepository services) : base(nameof(A03_Businesses), Stage.ScenarioCreation, 103, services, false, new BusinessCharts(services, Stage.ScenarioCreation))
        {
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<BusinessEntry>();
            var srcBusinessEntries = dbSrcHouses.Fetch<BusinessEntry>();
            if (srcBusinessEntries.Count == 0) {
                throw new FlaException("No srcBusinessEntries were found");
            }

            dbDstHouses.BeginTransaction();
            double factor = slice.EnergyReductionFactorBusiness;
            if (Math.Abs(factor) < 0.000001) {
                throw new FlaException("Factor 0 for energy reduction in slice " + slice);
            }
            foreach (var entry in srcBusinessEntries) {
                entry.ID = 0;
                entry.SetEnergyReduction(slice.DstYear.ToString(),entry.EffectiveEnergyDemand *(1- factor));
                entry.LocalnetEntries.Clear();
                dbDstHouses.Save(entry);
            }

            dbDstHouses.CompleteTransaction();
        }
    }
}