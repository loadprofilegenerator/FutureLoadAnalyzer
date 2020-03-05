using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._10_CrossSliceScenarioAnalysis {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class A03_EnergySplitBySupply : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A03_EnergySplitBySupply([NotNull] ServiceRepository services) : base(nameof(A03_EnergySplitBySupply),
            Stage.CrossSliceProfileAnalysis,
            103,
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
                var dbArchive =
                    Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
                var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive,
                    SaveableEntryTableType.SummedLoadsForAnalysis,
                    Services.Logger);
                var entries = saHouses.LoadAllOrMatching();
                var providerentries1 = entries.Where(x => x.Key.SumType == SumType.ByProvider).ToList();

                double electricitySum = 0;
                foreach (var entry in providerentries1) {
                    if (entry.GenerationOrLoad == GenerationOrLoad.Generation) {
                        continue;
                    }

                    double energy = entry.Profile.EnergySum();
                    string providertype = (entry.Key.ProviderType ?? throw new FlaException("No provider set")) + " " + entry.GenerationOrLoad;
                    Info("Providertype: " + providertype);
                    electricitySum += energy;
                }

                HouseComponentRepository hcr = new HouseComponentRepository(analysisRepo, slice);
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                Dictionary<string, double> loadPerEnergyType = new Dictionary<string, double>();
                Dictionary<string, double> genPerEnergyType = new Dictionary<string, double>();
                foreach (var house in houses) {
                    var components = house.CollectHouseComponents(hcr);
                    foreach (var component in components) {
                        if (component.HausAnschlussGuid == null) {
                            continue;
                        }

                        string energyType = component.EnergyType.ToString();
                        if (component.GenerationOrLoad == GenerationOrLoad.Load) {
                            if (!loadPerEnergyType.ContainsKey(energyType)) {
                                loadPerEnergyType.Add(energyType, 0);
                            }

                            loadPerEnergyType[energyType] += component.EffectiveEnergyDemand;
                        }
                        else if (component.GenerationOrLoad == GenerationOrLoad.Generation) {
                            if (!genPerEnergyType.ContainsKey(energyType)) {
                                genPerEnergyType.Add(energyType, 0);
                            }

                            genPerEnergyType[energyType] += component.EffectiveEnergyDemand;
                        }
                        else {
                            throw new FlaException("invalid type");
                        }
                    }
                }

                foreach (var pair in loadPerEnergyType) {
                    if (pair.Key == "Strom") {
                        myt[slice].AddValue("Jahresenergiebedarf [GWh]",
                            ChartHelpers.GetFriendlyEnergTypeName(pair.Key),
                            electricitySum,
                            DisplayUnit.GWh);
                    }
                    else {
                        myt[slice].AddValue("Jahresenergiebedarf [GWh]",
                            ChartHelpers.GetFriendlyEnergTypeName(pair.Key),
                            pair.Value,
                            DisplayUnit.GWh);
                    }
                }

                foreach (var pair in genPerEnergyType) {
                    if (pair.Key == "Strom") {
                        myt[slice].AddValue("GenerationPerEnergyType",
                            ChartHelpers.GetFriendlyEnergTypeName(pair.Key),
                            electricitySum,
                            DisplayUnit.GWh);
                    }
                    else {
                        myt[slice].AddValue("GenerationPerEnergyType", ChartHelpers.GetFriendlyEnergTypeName(pair.Key), pair.Value, DisplayUnit.GWh);
                    }
                }
            }

            var filename3 = MakeAndRegisterFullFilename("EnergieProEnergieträger.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(filename3, myt);
            SaveToPublicationDirectory(filename3, Constants.PresentSlice, "5");
        }
    }
}