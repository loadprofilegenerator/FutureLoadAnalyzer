using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.Sankey;

namespace FutureLoadAnalyzerLib._07_RawProfileVisualizing {
    // ReSharper disable once InconsistentNaming
    public class A_CitySumProfile : RunableWithBenchmark {
        public A_CitySumProfile([NotNull] ServiceRepository services) : base(nameof(A_CitySumProfile),
            Stage.RawProfileVisualisation,
            100,
            services,
            false)
        {
        }

        protected override void RunActualProcess()
        {
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbSrcProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var bkws = dbSrcProfiles.Fetch<BkwProfile>();
            var bkw = bkws[0];
            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            var emptyArr = new double[rlms[0].Profile.Values.Count];
            var allRlmsVals = new List<double>();
            allRlmsVals.AddRange(emptyArr);
            var allLocalElectricityVals = new List<double>();
            allLocalElectricityVals.AddRange(emptyArr);
            var allLocalElectricity = new Profile("Locally Generated", allLocalElectricityVals.AsReadOnly(), EnergyOrPower.Power);
            var allRlms = new Profile("All RLMs", allRlmsVals.AsReadOnly(), EnergyOrPower.Power);
            foreach (var rlm in rlms) {
                Profile profile = new Profile(rlm.Name, rlm.Profile.Values, rlm.Profile.EnergyOrPower);
                var onlyPos = profile.GetOnlyPositive(rlm.Name);

                if (onlyPos.Values.Sum() > 1) {
                    allRlms = allRlms.Add(onlyPos, "all Rlms");
                }
                else {
                    var onlyNeg = profile.GetOnlyNegative(rlm.Name);
                    allLocalElectricity = allLocalElectricity.Add(onlyNeg, "Locally Generated");
                }
            }

            var bkwProfile = new Profile(bkw.Profile);
            allLocalElectricity = allLocalElectricity.MultiplyWith(-1, "Locally Generated");
            var residual = bkwProfile.Minus(allRlms, "Residual");
            const double fac = 1_000_000;
            var rlmTotal = allRlms.EnergySum() / fac;
            var bkwTotal = bkwProfile.EnergySum() / fac;
            var localGenerationTotal = allLocalElectricity.EnergySum() / fac;
            var cityTotal = localGenerationTotal + bkwTotal;
            var residualTotal = residual.EnergySum() / fac + localGenerationTotal;
            var arrows = new List<SingleSankeyArrow>();
            var ssa1 = new SingleSankeyArrow("Erzeugung", 500, MyStage, SequenceNumber, Name, slice, Services);
            ssa1.AddEntry(new SankeyEntry("Lokale Erzeugung", localGenerationTotal, 200, Orientation.Up));
            ssa1.AddEntry(new SankeyEntry("BKW", bkwTotal, 200, Orientation.Straight));
            ssa1.AddEntry(new SankeyEntry("Burgdorf Strom [GWh]", cityTotal * -1, 200, Orientation.Straight));
            arrows.Add(ssa1);
            var ssa2 = new SingleSankeyArrow("Verbrauch", 500, MyStage, SequenceNumber, Name, slice, Services);
            ssa2.AddEntry(new SankeyEntry("", cityTotal, 200, Orientation.Straight));
            ssa2.AddEntry(new SankeyEntry("Residual", residualTotal * -1, 200, Orientation.Up));
            ssa2.AddEntry(new SankeyEntry("RLM", rlmTotal * -1, 200, Orientation.Straight));
            arrows.Add(ssa2);

            Services.PlotMaker.MakeSankeyChart(arrows);
            MakeBarCharts(allRlms, residual, allLocalElectricity);
        }

        private void MakeBarCharts([NotNull] Profile allRLms, [NotNull] Profile residual, [NotNull] Profile allLocalElectricity)
        {
            var allLs = new List<BarSeriesEntry>();
            var ls1 = allRLms.MakeHourlyAverages().GetBarSeries();
            allLs.Add(ls1);
            var ls2 = residual.MakeHourlyAverages().GetBarSeries();
            allLs.Add(ls2);

            var ls3 = allLocalElectricity.MakeHourlyAverages().GetBarSeries();
            allLs.Add(ls3);
            var filename = MakeAndRegisterFullFilename("FullCityProfileStack1.svg", Constants.PresentSlice);
            Services.PlotMaker.MakeBarChart(filename, "Leistung [kW]", allLs, new List<string>(), ExportType.SVG);
            foreach (var allL in allLs) {
                var filename2 = MakeAndRegisterFullFilename(allL.Name + ".svg", Constants.PresentSlice);
                var ns = new List<BarSeriesEntry> {
                    allL
                };
                Services.PlotMaker.MakeBarChart(filename2, "Leistung[kW]", ns, new List<string>(), ExportType.SVG);
            }
        }
    }
}