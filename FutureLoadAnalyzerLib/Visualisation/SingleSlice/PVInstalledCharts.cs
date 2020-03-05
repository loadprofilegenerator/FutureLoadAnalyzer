using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using Visualizer.Visualisation;
using Visualizer.Visualisation.SingleSlice;

namespace FutureLoadAnalyzerLib.Visualisation.SingleSlice
{
    public class PVInstalledCharts : VisualisationBase {
        public PVInstalledCharts([NotNull] IServiceRepository services, Stage mYStage) : base(nameof(PVPotentialCharts), services, mYStage)
        {
            DevelopmentStatus.Add("Maps are messed up");
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var pvSystems = dbHouse.Fetch<PvSystemEntry>();
            MakePvSystemSankey();
            MakePVMap();
            MakePvSystemComparison();

            void MakePvSystemSankey()
            {
                var ssa = new SingleSankeyArrow("HousePVSystems", 1500, MyStage, SequenceNumber, Name,
                     slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var count = pvSystems.Count;
                ssa.AddEntry(new SankeyEntry("PVSystems", count * -1, 2000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Kein PV", (houses.Count - count) * -1, 2000, Orientation.Straight));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakePvSystemComparison()
            {
                var ssa = new SingleSankeyArrow("RealPVSystemsVsPotential", 50, MyStage,
                    SequenceNumber, Name,  slice, Services);
                var houseGuidsWithPV = pvSystems.Select(x => x.HouseGuid).ToList();
                var potentials = dbHouse.Fetch<PVPotential>();
                var relevantPotentials = potentials.Where(x => houseGuidsWithPV.Contains(x.HouseGuid)).ToList();
                const double fac = 1_000_000;
                var potentialEnergy = relevantPotentials.Sum(x => x.SonnendachStromErtrag) / fac;
                var installedEnergy = pvSystems.Sum(x => x.EffectiveEnergyDemand)  / fac;
                ssa.AddEntry(new SankeyEntry("Sonnendach Potential auf Häusern mit PV", potentialEnergy, 20, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Installed", installedEnergy * -1, 20, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Verschwendetes Potential", (potentialEnergy - installedEnergy) * -1, 20, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakePVMap()
            {
                RGB GetColor(House h)
                {
                    if (pvSystems.Any(x => x.HouseGuid == h.Guid))
                    {
                        return Constants.Red;
                    }

                    return Constants.Black;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("PVSystems.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("PV", Constants.Red)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}
