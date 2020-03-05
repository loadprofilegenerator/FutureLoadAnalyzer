using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data;
using Data.DataModel;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace Visualizer.Visualisation.SingleSlice {
    public class PVPotentialCharts : VisualisationBase {
        public PVPotentialCharts([NotNull] IServiceRepository services, Stage myStage) : base(nameof(PVPotentialCharts), services, myStage)
        {
            DevelopmentStatus.Add("Maps are messed up");
            DevelopmentStatus.Add("clean up the different charts and remove redundants");
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var houses = dbHouses.Fetch<House>();
            var pvPotentials = dbHouses.Fetch<PVPotential>();
            var heatingsystems = dbHouses.Fetch<HeatingSystemEntry>();
            List<SonnenDach> sonnendach = dbRaw.Fetch<SonnenDach>();
            HashSet<string> houseGuidsForSystemsWithPV = new HashSet<string>();
            Dictionary<string, double> pvPotentialByHouseGuid = new Dictionary<string, double>();
            foreach (var house in houses) {
                pvPotentialByHouseGuid.Add(house.Guid, 0);
            }

            foreach (var pvpot in pvPotentials) {
                if (!houseGuidsForSystemsWithPV.Contains(pvpot.HouseGuid) && pvpot.SonnendachStromErtrag > 0) {
                    houseGuidsForSystemsWithPV.Add(pvpot.HouseGuid);
                }

                pvPotentialByHouseGuid[pvpot.HouseGuid] += pvpot.SonnendachStromErtrag;
            }

            PVPotentialSankey();
            MakePhotovoltaicDemandComparison();
            MakePvSystemSankey();
            MakePhotovoltaicSystemMap();
            MakePvPowerSankey();
            MakeHousePhotovotaikPoowerAufHousesMitLocalnet();
            MakeHousePhotovotaikAufHouses();
            MakeHousePhotovotaikAufHousesMitLocalnet();

            void PVPotentialSankey()
            {
                var ssa = new SingleSankeyArrow("PVPotentialSankey", 1000, MyStage, SequenceNumber, Name, slice, Services);
                const double fac = 1_000_000;
                double sonnendachPotential = sonnendach.Sum(x => x.stromertrag);
                ssa.AddEntry(new SankeyEntry("PVPotential", sonnendachPotential / fac, 5000, Orientation.Straight));
                var housesPotential = houses.Sum(x => pvPotentialByHouseGuid[x.Guid]);
                var diff = sonnendachPotential - housesPotential;
                ssa.AddEntry(new SankeyEntry("Auf Häusern", housesPotential * -1 / fac, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Sonstiges", diff / fac * -1, 5000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHousePhotovotaikAufHousesMitLocalnet()
            {
                var ssa = new SingleSankeyArrow("HouseSankeyFürPvZuordnungMitLocalnet",
                    1000,
                    MyStage,
                    SequenceNumber,
                    nameof(PVPotentialSankey),
                    slice,
                    Services);
                ssa.AddEntry(new SankeyEntry("Complexes", houses.Count, 5000, Orientation.Straight));
                var housesMitGebäude = houses.Where(x => houseGuidsForSystemsWithPV.Contains(x.Guid) && x.GebäudeObjectIDs.Count > 0).ToList();
                var countHousesMitGebäude = housesMitGebäude.Count;
                var housesTotal = houses.Count;
                ssa.AddEntry(new SankeyEntry("Gebäude mit Objektids und PV", countHousesMitGebäude * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Gebäude ohne Objektids und/oder ohne PV",
                    (housesTotal - countHousesMitGebäude) * -1,
                    5000,
                    Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHousePhotovotaikAufHouses()
            {
                var ssa = new SingleSankeyArrow("HouseSankeyFürPvZuordnung", 1000, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Complexes", houses.Count, 5000, Orientation.Straight));
                var housesMitGebäude = houses.Where(x => houseGuidsForSystemsWithPV.Contains(x.Guid)).ToList();
                var countHousesMitGebäude = housesMitGebäude.Count;
                var housesTotal = houses.Count;
                ssa.AddEntry(new SankeyEntry("Gebäude mit PV", countHousesMitGebäude * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Gebäude ohne PV", (housesTotal - countHousesMitGebäude) * -1, 5000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakePhotovoltaicSystemMap()
            {
                RGB GetColor(House h)
                {
                    if (pvPotentials.Any(x => x.HouseGuid == h.Guid)) {
                        return Constants.Green;
                    }

                    return Constants.Blue;
                }

                var mapColors = houses.Select(x => x.GetMapColorForHouse(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("PotentialPVSystems.png", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Hat PV Potential", Constants.Green),
                    new MapLegendEntry("Hat kein PV", Constants.Blue),
                    new MapLegendEntry("Hat kein Daten", Constants.Red)
                };
                Services.PlotMaker.MakeOsmMap(Name, filename, mapColors, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
                SaveToPublicationDirectory(filename, slice, "4");
            }

            void MakePvSystemSankey()
            {
                var ssa = new SingleSankeyArrow("HousePVPotentialSystems", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var houseguidsWithPV = pvPotentials.Select(x => x.HouseGuid).Distinct().ToList();
                ssa.AddEntry(new SankeyEntry("PVSystems", houseguidsWithPV.Count * -1, 2000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Kein PV", (houses.Count - houseguidsWithPV.Count) * -1, 2000, Orientation.Straight));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakePvPowerSankey()
            {
                var sonnendachRaw = dbRaw.Fetch<SonnenDach>();
                double rawSum = sonnendachRaw.Sum(x => x.stromertrag);
                const double fac = 1_000_000;
                var ssa = new SingleSankeyArrow("HousePVPowerSystems", 1500, MyStage, SequenceNumber, Name, slice, Services);
                var installed = pvPotentials.Sum(x => x.SonnendachStromErtrag);
                ssa.AddEntry(new SankeyEntry("Gesamt", rawSum / fac, 5000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("PV Systems", installed * -1 / fac, 2000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Kein PV", (rawSum - installed) * -1 / fac, 2000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeHousePhotovotaikPoowerAufHousesMitLocalnet()
            {
                var ssa = new SingleSankeyArrow("HouseSankeyFürPvPowerZuordnung", 1000, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Complexes", houses.Count, 5000, Orientation.Straight));
                var housesMitGebäude = houses.Where(x => houseGuidsForSystemsWithPV.Contains(x.Guid) && x.GebäudeObjectIDs.Count > 0).ToList();
                var countHousesMitGebäude = housesMitGebäude.Count;
                var housesTotal = houses.Count;
                ssa.AddEntry(new SankeyEntry("Gebäude mit Objektids und PV", countHousesMitGebäude * -1, 5000, Orientation.Up));
                ssa.AddEntry(
                    new SankeyEntry("Gebäude ohne Objektids und ohne PV", (housesTotal - countHousesMitGebäude) * -1, 5000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);

                RGB GetColor(House h)
                {
                    if (h.GebäudeObjectIDs.Count > 0 && houseGuidsForSystemsWithPV.Contains(h.Guid)) {
                        return new RGB(255, 0, 0);
                    }

                    if (h.GebäudeObjectIDs.Count > 0) {
                        return new RGB(0, 0, 255);
                    }

                    return new RGB(0, 255, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapPVWithLocalnet.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Localnet und Solar", 255, 0, 0),
                    new MapLegendEntry("Localnet Daten verfügbar", 0, 0, 255),
                    new MapLegendEntry("Rest", 0, 255, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }

            void MakePhotovoltaicDemandComparison()
            {
                var differenceInPower = new List<double>();
                foreach (var heatingSystemEntry in heatingsystems) {
                    if (heatingSystemEntry.OriginalHeatingSystemType == HeatingSystemType.GasheatingLocalnet) {
                        continue;
                    }

                    var pvpotentialentries = pvPotentials.Where(x => x.HouseGuid == heatingSystemEntry.HouseGuid).ToList();
                    var energysum = pvpotentialentries.Sum(x => x.SonnendachBedarfHeizung + x.SonnendachBedarfWarmwasser);
                    var h = houses.Single(x => x.Guid == heatingSystemEntry.HouseGuid);
                    if (energysum > 0 && heatingSystemEntry.EffectiveEnergyDemand > 0 && h.EnergieBezugsFläche > 0) {
                        var diff = (energysum - heatingSystemEntry.EffectiveEnergyDemand) / h.EnergieBezugsFläche;
                        if (diff > 500) {
                            diff = 500;
                        }

                        if (diff < -500) {
                            diff = -500;
                        }

                        differenceInPower.Add(diff);
                    }
                }

                differenceInPower.Sort();
                var barSeries = new List<BarSeriesEntry>();
                var bs = BarSeriesEntry.MakeBarSeriesEntry("differences");
                bs.Values.AddRange(differenceInPower);
                barSeries.Add(bs);
                var filename = MakeAndRegisterFullFilename("ComparisonSonnendachVsReal.svg", slice);
                var labes = new List<string>();
                Services.PlotMaker.MakeBarChart(filename, "ComparisonSonnendachVsReal", barSeries, labes, ExportType.SVG);
            }
        }
    }
}