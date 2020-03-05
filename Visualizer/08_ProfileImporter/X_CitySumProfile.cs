using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.Sankey;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class X_CitySumProfile : RunableWithBenchmark {
        public X_CitySumProfile([NotNull] ServiceRepository services)
            : base(nameof(X_CitySumProfile), Stage.ProfileImport, 2500, services, false)
        {
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            var bkws = dbSrcProfiles.Fetch<BkwProfile>();
            var bkw = bkws[0];
            var rlms = dbSrcProfiles.Fetch<RlmProfile>();
            var emptyArr = new double[rlms[0].Profile.Values.Count];
            var allRlmsVals = new List<double>();
            allRlmsVals.AddRange(emptyArr);
            var allLocalElectricityVals = new List<double>();
            allLocalElectricityVals.AddRange(emptyArr);
            var allLocalElectricity = new Profile("Locally Generated",
                allLocalElectricityVals.AsReadOnly(),ProfileType.Power);
            var allRlms = new Profile("All RLMs", allRlmsVals.AsReadOnly(),ProfileType.Power);
            foreach (var rlm in rlms) {
                var onlyPos = rlm.Profile.GetOnlyPositive(rlm.Name);

                if (onlyPos.Values.Sum() > 1) {
                    allRlms = allRlms.Add(onlyPos, "all Rlms");
                }
                else {
                    var onlyNeg = rlm.Profile.GetOnlyNegative(rlm.Name);
                    allLocalElectricity = allLocalElectricity.Add(onlyNeg, "Locally Generated");
                }
            }

            allLocalElectricity = allLocalElectricity.MultiplyWith(-1, "Locally Generated");
            var residual = bkw.Profile.MinusProfile(allRlms, "Residual");
            const double fac = 1_000_000;
            var rlmTotal = allRlms.EnergySum() / fac;
            var bkwTotal = bkw.Profile.EnergySum() / fac;
            var localGenerationTotal = allLocalElectricity.EnergySum() / fac;
            var cityTotal = localGenerationTotal + bkwTotal;
            var residualTotal = residual.EnergySum() / fac + localGenerationTotal;
            var arrows = new List<SingleSankeyArrow>();
            var ssa1 = new SingleSankeyArrow("Erzeugung", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
            ssa1.AddEntry(new SankeyEntry("Lokale Erzeugung", localGenerationTotal, 200, Orientation.Up));
            ssa1.AddEntry(new SankeyEntry("BKW", bkwTotal, 200, Orientation.Straight));
            ssa1.AddEntry(new SankeyEntry("Burgdorf Strom [GWh]", cityTotal * -1, 200, Orientation.Straight));
            arrows.Add(ssa1);
            var ssa2 = new SingleSankeyArrow("Verbrauch", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
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
            var filename = MakeAndRegisterFullFilename("FullCityProfileStack1.svg", Name, "", Constants.PresentSlice);
            Services.PlotMaker.MakeBarChart(filename, "Leistung [kW]", allLs, new List<string>(), ExportType.SVG);
            foreach (var allL in allLs) {
                var filename2 = MakeAndRegisterFullFilename(allL.Name + ".svg", allL.Name, "", Constants.PresentSlice);
                var ns = new List<BarSeriesEntry> {
                    allL
                };
                Services.PlotMaker.MakeBarChart(filename2, "Leistung[kW]", ns, new List<string>(), ExportType.SVG);
            }
        }

        protected override void RunActualProcess()
        {
        }
    }
}