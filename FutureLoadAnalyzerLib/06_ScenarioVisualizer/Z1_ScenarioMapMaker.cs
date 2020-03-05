using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    public class Z1_ScenarioMapMaker : RunnableForAllScenarioWithBenchmark {
        public Z1_ScenarioMapMaker([NotNull] ServiceRepository services) : base(nameof(Z1_ScenarioMapMaker),
            Stage.ScenarioVisualisation,
            2600,
            services,
            false)
        {
            DevelopmentStatus.Add("//todo: figure out etagenheizungen und color differently");
            DevelopmentStatus.Add("//todo: figure out kochgas and color differently");
            DevelopmentStatus.Add("//todo: reimplement the heating map using heating entries instead");
            DevelopmentStatus.Add("//todo: reimplement the heating map using heating entries instead");
            DevelopmentStatus.Add("//Map for water heaters");
            DevelopmentStatus.Add("//map for heating systems");
            DevelopmentStatus.Add("//map for cars");
            DevelopmentStatus.Add("//map for electric cars");
            DevelopmentStatus.Add("// map for houeseholds");
            DevelopmentStatus.Add("//map for number of appartments");
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,
                                                 [NotNull] AnalysisRepository analysisRepo)
        {
            if (!Services.RunningConfig.MakeCharts) {
                return;
            }

            foreach (ScenarioSliceParameters parameter in allSlices) {
                RunOneYear(parameter, analysisRepo);
            }
        }

        private void MakePVMap([NotNull] ScenarioSliceParameters slice, [NotNull] AnalysisRepository repo)
        {
            var pvSystems = repo.GetSlice(slice).Fetch<PvSystemEntry>();
            var houses = repo.GetSlice(slice).Fetch<House>();
            if (pvSystems.Count == 0) {
                return;
            }

            var adjustmentfactor = pvSystems.Max(x => x.EffectiveEnergyDemand) / 100;
            var pvPoints = new List<MapPoint>();
            foreach (var house in houses) {
                if (house.WgsGwrCoords.Count == 0) {
                    continue;
                }

                var co = house.WgsGwrCoords[0];
                var pvSystem = pvSystems.FirstOrDefault(x => x.HouseGuid == house.Guid);
                if (pvSystem != null) {
                    var radius = (int)(pvSystem.EffectiveEnergyDemand / adjustmentfactor);
                    if (radius < 10) {
                        radius = 10;
                    }

                    pvPoints.Add(new MapPoint(co.Lat, co.Lon, radius, 255, 0, 0));
                }
                else {
                    pvPoints.Add(new MapPoint(co.Lat, co.Lon, 10, 0, 0, 0));
                }
            }

            var dstFileName2 = MakeAndRegisterFullFilename(slice.GetFileName() + "_PV_power_Map.svg", slice);
            var lge = new List<MapLegendEntry> {
                new MapLegendEntry(slice.DstScenario + " Jahr " + slice.DstYear, Constants.Black)
            };
            Services.MapDrawer.DrawMapSvg(pvPoints, dstFileName2, lge);
            Info("PV Maps written");
        }

        private void RunOneYear([NotNull] ScenarioSliceParameters slice, [NotNull] AnalysisRepository repo)
        {
            MakePVMap(slice, repo);
        }
    }
}