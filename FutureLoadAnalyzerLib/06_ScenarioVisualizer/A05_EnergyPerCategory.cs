using System.Collections.Generic;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class A05_EnergyPerCategory : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A05_EnergyPerCategory([NotNull] ServiceRepository services) : base(nameof(A05_EnergyPerCategory),
            Stage.ScenarioVisualisation,
            105,
            services,
            true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,
                                                 [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make trafostation results");
            MultiyearMultiVariableTrend myt = new MultiyearMultiVariableTrend();
            foreach (var slice in allSlices) {
                HouseComponentRepository hcr = new HouseComponentRepository(analysisRepo, slice);
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                Dictionary<string, double> loadPerEnergyType = new Dictionary<string, double>();
                Dictionary<string, double> genPerEnergyType = new Dictionary<string, double>();
                Dictionary<string, double> loadPerComponentType = new Dictionary<string, double>();
                Dictionary<string, double> genPerPerComponentType = new Dictionary<string, double>();
                foreach (var house in houses) {
                    var components = house.CollectHouseComponents(hcr);
                    foreach (var component in components) {
                        if (component.HausAnschlussGuid == null) {
                            continue;
                        }

                        string energyType = component.EnergyType.ToString();
                        string componentType = component.HouseComponentType + " - " + energyType;
                        if (component.GenerationOrLoad == GenerationOrLoad.Load) {
                            if (!loadPerEnergyType.ContainsKey(energyType)) {
                                loadPerEnergyType.Add(energyType, 0);
                            }

                            loadPerEnergyType[energyType] += component.EffectiveEnergyDemand;
                            if (!loadPerComponentType.ContainsKey(componentType)) {
                                loadPerComponentType.Add(componentType, 0);
                            }

                            loadPerComponentType[componentType] += component.EffectiveEnergyDemand;
                        }
                        else if (component.GenerationOrLoad == GenerationOrLoad.Generation) {
                            if (!genPerEnergyType.ContainsKey(energyType)) {
                                genPerEnergyType.Add(energyType, 0);
                            }

                            genPerEnergyType[energyType] += component.EffectiveEnergyDemand;

                            if (!genPerPerComponentType.ContainsKey(componentType)) {
                                genPerPerComponentType.Add(componentType, 0);
                            }

                            genPerPerComponentType[componentType] += component.EffectiveEnergyDemand;
                        }
                        else {
                            throw new FlaException("invalid type");
                        }
                    }
                }

                foreach (var pair in loadPerEnergyType) {
                    myt[slice].AddValue("LoadPerEnergyType", pair.Key, pair.Value, DisplayUnit.GWh);
                }

                foreach (var pair in genPerEnergyType) {
                    myt[slice].AddValue("GenerationPerEnergyType", pair.Key, pair.Value, DisplayUnit.GWh);
                }

                loadPerComponentType = Helpers.MakeSortedDictionary(loadPerComponentType);

                foreach (var pair in loadPerComponentType) {
                    myt[slice].AddValue("LoadPerComponentType", pair.Key, pair.Value, DisplayUnit.GWh);
                }

                genPerPerComponentType = Helpers.MakeSortedDictionary(genPerPerComponentType);
                foreach (var pair in genPerPerComponentType) {
                    myt[slice].AddValue("GenerationPerComponentType", pair.Key, pair.Value, DisplayUnit.GWh);
                }
            }

            var filename3 = MakeAndRegisterFullFilename("TotalEnergyResultsAreaCharts.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(filename3, myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
        }
    }
}