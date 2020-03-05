using System.Collections.Generic;
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
    public class A07_HeatingSystemTrends : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A07_HeatingSystemTrends([NotNull] ServiceRepository services) : base(nameof(A07_HeatingSystemTrends),
            Stage.ScenarioVisualisation,
            107,
            services,
            true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,
                                                 [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make heating system results");
            MultiyearMultiVariableTrend myt = new MultiyearMultiVariableTrend();
            foreach (var slice in allSlices) {
                var heating = analysisRepo.GetSlice(slice).Fetch<HeatingSystemEntry>();
                if (heating.Count == 0) {
                    throw new FlaException("no heating entries for " + slice);
                }

                Dictionary<HeatingSystemType, double> heatDemandByType = new Dictionary<HeatingSystemType, double>();
                Dictionary<HeatingSystemType, double> energyDemandByType = new Dictionary<HeatingSystemType, double>();
                Dictionary<HeatingSystemType, double> countByType = new Dictionary<HeatingSystemType, double>();
                Dictionary<int, double> heatDemandByYear = new Dictionary<int, double>();
                foreach (var hse in heating) {
                    if (!heatDemandByType.ContainsKey(hse.SynthesizedHeatingSystemType)) {
                        heatDemandByType.Add(hse.SynthesizedHeatingSystemType, 0);
                        energyDemandByType.Add(hse.SynthesizedHeatingSystemType, 0);
                        countByType.Add(hse.SynthesizedHeatingSystemType, 0);
                    }

                    heatDemandByType[hse.SynthesizedHeatingSystemType] += hse.HeatDemand;
                    energyDemandByType[hse.SynthesizedHeatingSystemType] += hse.EffectiveEnergyDemand;
                    countByType[hse.SynthesizedHeatingSystemType] += 1;
                    foreach (var demand in hse.HeatDemands) {
                        if (!heatDemandByYear.ContainsKey(demand.Year)) {
                            heatDemandByYear.Add(demand.Year, 0);
                        }

                        heatDemandByYear[demand.Year] += demand.HeatDemand;
                    }
                }

                foreach (var pair in heatDemandByType) {
                    myt[slice].AddValue("HeatDemand", pair.Key.ToString(), pair.Value, DisplayUnit.GWh);
                }

                foreach (var pair in energyDemandByType) {
                    myt[slice].AddValue("Energy Demand", pair.Key.ToString(), pair.Value, DisplayUnit.GWh);
                }

                foreach (var pair in countByType) {
                    myt[slice].AddValue("Count", pair.Key.ToString(), pair.Value, DisplayUnit.Stk);
                }

                foreach (var pair in heatDemandByYear) {
                    myt[slice].AddValue("HeatDemandByYear", pair.Key.ToString(), pair.Value, DisplayUnit.GWh);
                }
            }

            var filename3 = MakeAndRegisterFullFilename("HeatingSystemTrends.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(filename3, myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
        }
    }
}