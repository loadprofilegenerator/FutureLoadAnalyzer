using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class A08_AirConditioning : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A08_AirConditioning([NotNull] ServiceRepository services)
            : base(nameof(A08_AirConditioning), Stage.ScenarioVisualisation, 108, services,true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make house results");
            MultiyearTrend myt = new MultiyearTrend();
            foreach (var slice in allSlices) {
                var airCons = analysisRepo.GetSlice(slice).Fetch<AirConditioningEntry>();
                myt[slice].AddValue("Klimaanlagen", airCons.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Klimaanlagen Stromverbrauch", airCons.Sum(x=> x.EffectiveEnergyDemand), DisplayUnit.GWh);
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                double percentage =(double) airCons.Count / houses.Count;
                myt[slice].AddValue("Anteil Häuser mit Klimaanlage",percentage, DisplayUnit.Stk);
                var houseGuidsWithAircon = airCons.ToReferenceGuidHashset(x => x.HouseGuid);
                var housesWithAirCon = houses.Where(x => houseGuidsWithAircon.Contains(x.HouseGuid));
                double totalebf = houses.Sum(x => x.EnergieBezugsFläche);
                double airConEbf = housesWithAirCon.Sum(x => x.EnergieBezugsFläche);
                myt[slice].AddValue("Anteil EBF mit Klimaanlage", airConEbf/totalebf, DisplayUnit.Stk);
            }
            var filename3 = MakeAndRegisterFullFilename("AirConditioning.xlsx",Constants.PresentSlice);
            Info("Writing results to " + filename3);

            XlsxDumper.DumpMultiyearTrendToExcel(filename3,myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
        }
    }
}