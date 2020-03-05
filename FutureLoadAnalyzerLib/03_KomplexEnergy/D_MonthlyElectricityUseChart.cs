using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer;

namespace FutureLoadAnalyzerLib._03_KomplexEnergy {
    // ReSharper disable once InconsistentNaming
    public class D_MonthlyElectricityUseChart : RunableWithBenchmark {
        public D_MonthlyElectricityUseChart([NotNull] ServiceRepository services) : base(nameof(D_MonthlyElectricityUseChart),
            Stage.ComplexEnergyData,
            4,
            services,
            true)
        {
        }

        public void CreateChart()
        {
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            var energyusePerStandort = db.Fetch<MonthlyElectricityUsePerStandort>();
            var vbas = new List<Verbrauchsart> {
                Verbrauchsart.ElectricityLocalnet,
                Verbrauchsart.ElectricityNetz,
                // Verbrauchsart.Netzuebergabe,
                Verbrauchsart.Gas,
                Verbrauchsart.Fernwaerme
            };
            MakeSortedChart(energyusePerStandort);
            foreach (var vba in vbas) {
                MakeChartForOneVerbrauchsart(vba, 1000, "MWh", energyusePerStandort, db);
            }
        }

        protected override void RunActualProcess()
        {
            CreateChart();
        }

        [NotNull]
        private static Dictionary<string, Dictionary<int, double>> GetDictForVerbrauch(
            [NotNull] MonthlyElectricityUsePerStandort perStandortElectricityMonthlyValuesByTarif,
            Verbrauchsart vba)
        {
            switch (vba) {
                case Verbrauchsart.ElectricityLocalnet:
                    return perStandortElectricityMonthlyValuesByTarif.ElectricityLocalNetMonthlyValuesByTarif;
                case Verbrauchsart.Gas:
                    return perStandortElectricityMonthlyValuesByTarif.GasMonthlyValuesByTarif;
                case Verbrauchsart.Fernwaerme:
                    return perStandortElectricityMonthlyValuesByTarif.FernwaermeMonthlyValuesByTarif;
                case Verbrauchsart.ElectricityNetz:

                    return perStandortElectricityMonthlyValuesByTarif.ElectricityNetzMonthlyValuesByTarif;
                case Verbrauchsart.Netzuebergabe:
                    return perStandortElectricityMonthlyValuesByTarif.ElectricityNetzübergabeMonthlyValuesByTarif;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vba), vba, null);
            }
        }

        private void MakeChartForOneVerbrauchsart(Verbrauchsart vba,
                                                  double factor,
                                                  [NotNull] string unit,
                                                  [NotNull] [ItemNotNull] List<MonthlyElectricityUsePerStandort> energyusePerStandort,
                                                  [NotNull] MyDb db)
        {
            var path = db.GetResultFullPath(SequenceNumber, Name);
            var energyUseByTarifAndMonth = new Dictionary<string, Dictionary<int, double>>();
            foreach (var perStandort in energyusePerStandort) {
                var dict = GetDictForVerbrauch(perStandort, vba);
                foreach (var tarifValues in dict) {
                    if (!energyUseByTarifAndMonth.ContainsKey(tarifValues.Key)) {
                        energyUseByTarifAndMonth.Add(tarifValues.Key, new Dictionary<int, double>());
                        for (var i = 1; i < 13; i++) {
                            energyUseByTarifAndMonth[tarifValues.Key].Add(i, 0);
                        }
                    }

                    foreach (var monthlyValues in tarifValues.Value) {
                        energyUseByTarifAndMonth[tarifValues.Key][monthlyValues.Key] += monthlyValues.Value / factor;
                    }
                }
            }

            //anzahl gebäude
            var bses = new List<BarSeriesEntry>();
            var dstPath = Path.Combine(path, "allData." + vba + ".csv");
            using (var sw = new StreamWriter(dstPath)) {
                var sb = new StringBuilder();
                //header
                foreach (var tarifValues in energyUseByTarifAndMonth) {
                    sb.Append(tarifValues.Key).Append(";");
                }

                sw.WriteLine(sb);
                for (var i = 1; i < 13; i++) {
                    sb = new StringBuilder();
                    foreach (var tarifValues in energyUseByTarifAndMonth) {
                        sb.Append(tarifValues.Value[i]).Append(";");
                    }

                    sw.WriteLine(sb);
                }

                sw.Close();
            }

            foreach (var tarifValues in energyUseByTarifAndMonth) {
                var bse = new BarSeriesEntry(tarifValues.Key);
                bse.Values.Add(0);
                for (var i = 0; i < 12; i++) {
                    bse.Values.Add(tarifValues.Value[i + 1]);
                }

                bses.Add(bse);
                var singleTarif = new List<BarSeriesEntry> {
                    bse
                };
                Services.PlotMaker.MakeBarChart(Path.Combine(path, "MonthlyUse." + vba + "." + FilenameHelpers.CleanFileName(bse.Name) + ".png"),
                    "Strom [kWh]",
                    singleTarif,
                    new List<string>());
            }

            Services.PlotMaker.MakeBarChart(Path.Combine(path, "MonthlyUse." + vba + ".png"), "Strom [" + unit + "]", bses, new List<string>());
        }

        private void MakeSortedChart([NotNull] [ItemNotNull] List<MonthlyElectricityUsePerStandort> energyusePerStandort)
        {
            var electricityEntriesWithValues = energyusePerStandort.Where(x => x.YearlyElectricityUseLocalnet > 0).ToList();
            electricityEntriesWithValues.Sort((x, y) => y.YearlyElectricityUseLocalnet.CompareTo(x.YearlyElectricityUseLocalnet));
            var gasEntriesWithValues = energyusePerStandort.Where(x => x.YearlyGasUse > 0).ToList();
            gasEntriesWithValues.Sort((x, y) => y.YearlyGasUse.CompareTo(x.YearlyGasUse));

            {
                var pas = new List<PlannedAnnotations>();
                var zeroAnno = new PlannedAnnotations(0, 500, 1, 100);
                pas.Add(new PlannedAnnotations(500, 1000, 1, 100));
                pas.Add(new PlannedAnnotations(1000, 2500, 1, 100));
                pas.Add(new PlannedAnnotations(2500, 5000, -1, 150));
                pas.Add(new PlannedAnnotations(5000, 10000, -1, 200));
                pas.Add(new PlannedAnnotations(10000, 50000, -1, 50));
                pas.Add(new PlannedAnnotations(50000, 1000000000, -1, 200));
                var bses = new List<LineSeriesEntry>();
                var lse = new LineSeriesEntry("Electricity");
                var idx = 0;
                double sum = 0;
                var electricityPaes = new List<AnnotationEntry>();
                foreach (var standort in electricityEntriesWithValues) {
                    sum += standort.YearlyElectricityUseLocalnet;
                    foreach (var plannedAnnotationse in pas) {
                        if (standort.YearlyElectricityUseLocalnet < plannedAnnotationse.Limit && !plannedAnnotationse.Processed) {
                            electricityPaes.Add(new AnnotationEntry(plannedAnnotationse.MakeDescription(electricityEntriesWithValues),
                                idx,
                                sum,
                                plannedAnnotationse.Direction,
                                plannedAnnotationse.YOffset));
                            plannedAnnotationse.Processed = true;
                        }
                    }

                    lse.Values.Add(new Point(idx, sum));
                    idx++;
                }

                electricityPaes.Add(new AnnotationEntry(zeroAnno.MakeDescription(electricityEntriesWithValues), idx, sum, 1, 50));
                bses.Add(lse);
                var fn = MakeAndRegisterFullFilename("SortedCustomers.Electricity.png", Constants.PresentSlice);
                Services.PlotMaker.MakeLineChart(fn, "Strom [kWh]", bses, electricityPaes);
            }
            {
                var pas = new List<PlannedAnnotations>();
                var zeroAnno = new PlannedAnnotations(0, 500, 1, 100);
                pas.Add(new PlannedAnnotations(500, 1000, 1, 100));
                pas.Add(new PlannedAnnotations(1000, 10000, -1, 200));
                pas.Add(new PlannedAnnotations(10000, 50000, -1, 50));
                pas.Add(new PlannedAnnotations(50000, 1000000000, -1, 200));
                var bses = new List<LineSeriesEntry>();
                var lse = new LineSeriesEntry("Electricity");
                var gasPaes = new List<AnnotationEntry>();
                var idx = 0;
                double sum = 0;
                foreach (var standort in gasEntriesWithValues) {
                    sum += standort.YearlyGasUse;
                    foreach (var plannedAnnotationse in pas) {
                        if (standort.YearlyGasUse < plannedAnnotationse.Limit && !plannedAnnotationse.Processed) {
                            gasPaes.Add(new AnnotationEntry(plannedAnnotationse.MakeDescription(gasEntriesWithValues),
                                idx,
                                sum,
                                plannedAnnotationse.Direction,
                                plannedAnnotationse.YOffset));
                            plannedAnnotationse.Processed = true;
                        }
                    }

                    lse.Values.Add(new Point(idx, sum));
                    idx++;
                }

                gasPaes.Add(new AnnotationEntry(zeroAnno.MakeDescription(gasEntriesWithValues), idx, sum, 1, 50));
                bses.Add(lse);
                var fn2 = MakeAndRegisterFullFilename("SortedCustomers.Gas.png", Constants.PresentSlice);
                Services.PlotMaker.MakeLineChart(fn2, "Strom [kWh]", bses, gasPaes);
            }
        }

        private class PlannedAnnotations {
            public PlannedAnnotations(double limit, double nextLimit, double direction, double yOffset)
            {
                Limit = limit;
                NextLimit = nextLimit;
                Direction = 1;
                YOffset = 100;
                Direction = direction;
                YOffset = yOffset;
            }

            public double Direction { get; }

            public double Limit { get; }
            public double NextLimit { get; }
            public bool Processed { get; set; }
            public double YOffset { get; }

            [NotNull]
            public string MakeDescription([NotNull] [ItemNotNull] List<MonthlyElectricityUsePerStandort> monthlyElectricityUsePerStandorts)
            {
                var l = monthlyElectricityUsePerStandorts
                    .Where(x => x.YearlyElectricityUseLocalnet >= Limit && x.YearlyElectricityUseLocalnet < NextLimit).ToList();
                return "Anzahl Kunden\nzwischen " + Limit + " und " + NextLimit + " kWh:\n" + l.Count;
            }
        }
    }
}