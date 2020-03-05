using System;
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
    public class A02_HouseResultsXls : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A02_HouseResultsXls([NotNull] ServiceRepository services) : base(nameof(A02_HouseResultsXls),
            Stage.ScenarioVisualisation,
            101,
            services,
            false)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,
                                                 [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make house results");
            MakePopulationResults(allSlices, analysisRepo);
            MakeAreaResults(allSlices, analysisRepo);
        }

        private void MakeAreaResults([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            MultiyearMultiVariableTrend mymvt = new MultiyearMultiVariableTrend();
            foreach (var slice in allSlices) {
                Dictionary<int, double> areaByYear = new Dictionary<int, double>();
                Dictionary<int, int> countByYear = new Dictionary<int, int>();
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                foreach (var house in houses) {
                    foreach (var app in house.Appartments) {
                        if (!app.IsApartment) {
                            continue;
                        }

                        if (!areaByYear.ContainsKey(app.Year)) {
                            areaByYear.Add(app.Year, 0);
                            countByYear.Add(app.Year, 0);
                        }

                        areaByYear[app.Year] += app.EnergieBezugsFläche;
                        countByYear[app.Year]++;
                    }
                }

                foreach (var pair in areaByYear) {
                    mymvt[slice].AddValue("Area", pair.Key.ToString(), pair.Value, DisplayUnit.Stk);
                }

                foreach (var pair in countByYear) {
                    mymvt[slice].AddValue("Anzahl Appartments", pair.Key.ToString(), pair.Value, DisplayUnit.Stk);
                }
            }

            var filename4 = MakeAndRegisterFullFilename("Hausflächen.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename4);
            XlsxDumper.DumpMultiyearMultiVariableTrendToExcel(filename4, mymvt);
            SaveToArchiveDirectory(filename4, RelativeDirectory.Report, Constants.PresentSlice);
        }

        private void MakePopulationResults([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            MultiyearTrend myt = new MultiyearTrend();
            foreach (var slice in allSlices) {
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                var households = analysisRepo.GetSlice(slice).Fetch<Household>();
                var occupants = households.SelectMany(x => x.Occupants).ToList();
                myt[slice].AddValue("Anzahl Häuser", houses.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Anzahl Haushalte", households.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Bewohner", occupants.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Durchschnittsalter", occupants.Average(x => x.Age), DisplayUnit.Stk);
                var male = occupants.Count(x => x.Gender == Gender.Male);
                myt[slice].AddValue("Geschlecht", male / (double)occupants.Count, DisplayUnit.Percentage);
                var appartments = houses.Sum(x => x.OfficialNumberOfHouseholds);
                myt[slice].AddValue("Gesamtanzahl Wohnungen Ebbe", appartments, DisplayUnit.Stk);

                var leereWohnungenperHouse = houses.Sum(x =>
                    Math.Max(x.OfficialNumberOfHouseholds -
                             households.GetByReferenceGuidWithEmptyReturns(x.HouseGuid, "HouseGuid", y => y.HouseGuid).Count,
                        0));
                myt[slice].AddValue("Leerstand Ebbe", leereWohnungenperHouse, DisplayUnit.Stk);
                myt[slice].AddValue("Leerstand Stadtweit", appartments - households.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Durchschnitt. Haushaltsgrösse", households.Average(x => x.Occupants.Count), DisplayUnit.Stk);
                myt[slice].AddValue("Energiebezugsfläche", houses.Sum(x => x.EnergieBezugsFläche), DisplayUnit.Stk);
                myt[slice].AddValue("Durch. Energiebezugsfläche", houses.Average(x => x.EnergieBezugsFläche), DisplayUnit.Stk);
            }

            var filename3 = MakeAndRegisterFullFilename("PopulationResults.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearTrendToExcel(filename3, myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
            SaveToPublicationDirectory(filename3, Constants.PresentSlice, "4.4");
        }
    }
}