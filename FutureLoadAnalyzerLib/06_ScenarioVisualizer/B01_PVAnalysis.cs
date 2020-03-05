using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class B01_PVAnalysis : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public B01_PVAnalysis([NotNull] ServiceRepository services)
            : base(nameof(B01_PVAnalysis), Stage.ScenarioVisualisation, 201, services,true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make house results");
            var sheets = new List<RowCollection>();
            foreach (var slice in allSlices) {
                RowCollection rc = new RowCollection(slice.DstScenario.ShortName + "_" + slice.DstYear, slice.DstScenario.Name + " - " + slice.DstYear);
                var pventries = analysisRepo.GetSlice(slice).Fetch<PvSystemEntry>();
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                var pvPotential = analysisRepo.GetSlice(slice).Fetch<PVPotential>();
                foreach (var house in houses) {
                    var pventriesInHouse = pventries.GetByReferenceGuidWithEmptyReturns(house.HouseGuid, "HouseGuid",y=> y.HouseGuid);
                    var rb = RowBuilder.Start("House",house.ComplexName);
                    var pvPotentialsInHouse = pvPotential.GetByReferenceGuidWithEmptyReturns(house.HouseGuid, "HouseGuid",y=> y.HouseGuid);
                    rb.Add("PV entries", pventriesInHouse.Count);
                    if (pventriesInHouse.Count > 0) {
                        rb.Add("PV Year", pventriesInHouse.Max(x => x.BuildYear));
                    }

                    rb.Add("EnergySum", pventriesInHouse.Sum(x => x.EffectiveEnergyDemand));
                    rb.Add("Potential Count", pvPotentialsInHouse.Count);
                    rb.Add("Potential Energy Sum", pvPotentialsInHouse.Sum(x => x.SonnendachStromErtrag));
                    rc.Add(rb);
                }
                sheets.Add(rc);
            }

            var fn = MakeAndRegisterFullFilename("PVAnalysis.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn, sheets);
        }
    }
}