using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
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
    public class A01_EnergyPerCategory : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A01_EnergyPerCategory([NotNull] ServiceRepository services)
            : base(nameof(A01_EnergyPerCategory), Stage.CrossSliceProfileAnalysis, 101, services,true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            List<ScenarioSliceParameters> missingSlices = new List<ScenarioSliceParameters>();
            foreach (var slice in allSlices) {
                Info("Checking for slice " + slice);
                var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice);
                var fi = new FileInfo(db.DBFilename);
                if (!fi.Exists) {
                    missingSlices.Add(slice);
                }
            }
            if (missingSlices.Count > 0) {
                var missingSliceNames = missingSlices.Select(x => x.ToString()).ToList();
                string missingSlicesStr = string.Join("\n", missingSliceNames);
                throw new FlaException("Missing Profile Slices: " + missingSlicesStr);
            }

            Info("starting to make trafostation results");
            MultiyearMultiVariableTrend mytProviders = new MultiyearMultiVariableTrend();
            MultiyearMultiVariableTrend mytComponents = new MultiyearMultiVariableTrend();
            foreach (var slice in allSlices) {
                var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
                var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
                var entries = saHouses.LoadAllOrMatching();
                var providerentries1 = entries.Where(x => x.Key.SumType == SumType.ByProvider).ToList();
                providerentries1.Sort((x, y) => String.Compare(x.Key.ProviderType, y.Key.ProviderType, StringComparison.Ordinal));
                Dictionary<Tuple<string, GenerationOrLoad>, double> energyByName = new Dictionary<Tuple<string, GenerationOrLoad>, double>();
                foreach (var entry in providerentries1) {
                    double energy = entry.Profile.EnergySum();
                    string providertype =( entry.Key.ProviderType ?? throw new FlaException("No provider set")) + " " + entry.GenerationOrLoad;
                    Info("Providertype: " + providertype);
                    var friendlyName = ChartHelpers.GetFriendlyProviderName(providertype);
                    var key = new Tuple<string, GenerationOrLoad>(friendlyName, entry.GenerationOrLoad);
                    if (!energyByName.ContainsKey(key)) {
                        energyByName.Add(key, 0);
                    }
                    energyByName[key] += energy;
                }

                foreach (var pair in energyByName) {
                    if (pair.Key.Item2 == GenerationOrLoad.Load) {
                        mytProviders[slice].AddValue("Stromlast [GWh]", pair.Key.Item1, pair.Value, DisplayUnit.GWh);
                    }
                    else {
                        mytProviders[slice].AddValue("Erzeugung [GWh]", pair.Key.Item1, pair.Value, DisplayUnit.GWh);
                    }
                }

                var componentEntries = entries.Where(x => x.Key.SumType == SumType.ByHouseholdComponentType).ToList();
                componentEntries.Sort((x,y)=> String.Compare(x.Key.HouseComponentType, y.Key.HouseComponentType, StringComparison.Ordinal));
                foreach (var entry in componentEntries) {
                    double energy = entry.Profile.EnergySum();
                    string houseComponentType = entry.Key.HouseComponentType ?? throw new FlaException("No provider set");
                    Info("HouseComponentType: " + houseComponentType);
                    if (entry.Key.GenerationOrLoad == GenerationOrLoad.Load) {
                        mytComponents[slice].AddValue("ComponentLoad", houseComponentType, energy, DisplayUnit.GWh);
                    }
                    else {
                        mytComponents[slice].AddValue("ComponentGeneration", houseComponentType, energy, DisplayUnit.GWh);
                    }
                }
            }
            var filename3 = MakeAndRegisterFullFilename("EnergieProVerbraucher.xlsx",Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(filename3, mytProviders);
            SaveToPublicationDirectory(filename3,Constants.PresentSlice,"5");
            var filename4 = MakeAndRegisterFullFilename("ComponentProfileEnergyCharts.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename4);
            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(filename4, mytComponents);
            SaveToArchiveDirectory(filename4,RelativeDirectory.Report,Constants.PresentSlice);
        }
    }
}