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
    public class A04_Trafostationen : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A04_Trafostationen([NotNull] ServiceRepository services)
            : base(nameof(A04_Trafostationen), Stage.ScenarioVisualisation, 104, services,true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make trafostation results");
            MultiyearTrend myt = new MultiyearTrend();
            foreach (var slice in allSlices) {

                HouseComponentRepository hcr = new HouseComponentRepository(analysisRepo,slice);
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                Dictionary<string,double> energyUsePerTrafostation = new Dictionary<string, double>();
                var hausanschlusses = analysisRepo.GetSlice(slice).Fetch<Hausanschluss>();
                foreach (var house in houses) {
                    var components = house.CollectHouseComponents(hcr);
                    foreach (var component in components) {
                        if (component.HausAnschlussGuid == null) {
                            continue;
                        }
                        var hausanschluss = hausanschlusses.GetByGuid(component.HausAnschlussGuid);
                        var trafo = hausanschluss.Trafokreis;
                        if (!energyUsePerTrafostation.ContainsKey(trafo)) {
                            energyUsePerTrafostation.Add(trafo,0);
                        }

                        energyUsePerTrafostation[trafo] += component.EffectiveEnergyDemand;
                    }
                }

                double totalEnergy = 0;
                foreach (var energy in energyUsePerTrafostation) {
                    myt[slice].AddValue(energy.Key,energy.Value, DisplayUnit.GWh);
                    totalEnergy += energy.Value;
                }
                myt[slice].AddValue("Total", totalEnergy, DisplayUnit.GWh);
            }
            var filename3 = MakeAndRegisterFullFilename("TrafostationEnergyResults.xlsx",Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearTrendToExcel(filename3,myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
        }
    }
}