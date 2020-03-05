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
    public class A06_CarCharts : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A06_CarCharts([NotNull] ServiceRepository services) : base(nameof(A06_CarCharts), Stage.ScenarioVisualisation, 106, services, true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,
                                                 [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make Car results");
            MultiyearTrend myt = new MultiyearTrend();
            foreach (var slice in allSlices) {
                var cars = analysisRepo.GetSlice(slice).Fetch<Car>();
                var carDistanceEntries = analysisRepo.GetSlice(slice).Fetch<CarDistanceEntry>();
                if (carDistanceEntries.Count != cars.Count) {
                    throw new FlaException("Cars and car distance entries count don't match");
                }

                myt[slice].AddValue("Cars", cars.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Cars Electric", cars.Count(x => x.CarType == CarType.Electric), DisplayUnit.Stk);
                myt[slice].AddValue("Cars Gasoline", cars.Count(x => x.CarType == CarType.Gasoline), DisplayUnit.Stk);
                myt[slice].AddValue("Avg Kilometers Per Car", carDistanceEntries.Average(x => x.DistanceEstimate), DisplayUnit.Stk);
                myt[slice].AddValue("Avg Energy Estimate Per car", carDistanceEntries.Average(x => x.EnergyEstimate), DisplayUnit.Stk);
                var electricCars = cars.Where(x => x.CarType == CarType.Electric).Select(x => x.Guid).ToHashSet();
                var electricCarDistanceEntries = carDistanceEntries.Where(x => electricCars.Contains(x.CarGuid)).ToList();
                myt[slice].AddValue("Summed Electric Energy Estimate Per car",
                    electricCarDistanceEntries.Sum(x => x.EnergyEstimate),
                    DisplayUnit.GWh);
                myt[slice].AddValue("Car Distance Entries", carDistanceEntries.Count, DisplayUnit.Stk);
            }

            var filename3 = MakeAndRegisterFullFilename("TotalCarDevelopment.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearTrendToExcel(filename3, myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
        }
    }
}