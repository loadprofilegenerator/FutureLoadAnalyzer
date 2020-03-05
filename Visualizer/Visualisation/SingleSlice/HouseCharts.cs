using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using MathNet.Numerics.Statistics;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace Visualizer.Visualisation.SingleSlice {
    public class HouseCharts : VisualisationBase {
        public HouseCharts([NotNull] IServiceRepository services, Stage myStage) : base(nameof(HouseCharts), services, myStage)
        {
        }

        protected override void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent)
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            var houses = dbHouse.Fetch<House>();
            if (houses.Count == 0) {
                throw new FlaException("No houses were found in the data");
            }

            if (isPresent) {
                var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
                var dbComplex = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
                var complexes = dbComplex.Fetch<BuildingComplex>();
                MakeHouseKey(houses, complexes, slice);

                var gwr = dbRaw.Fetch<GwrData>();
                Debug("Loaded gwr data: " + gwr.Count);
                var allHouseholds = dbHouse.Fetch<PotentialHousehold>();
                Info("Loaded haushalte " + allHouseholds.Count);
                MakeOtherHouseChart(allHouseholds, gwr, complexes, houses, slice);

            }

            AgeHistogram();
            MakeHouseGebäudeResultsKey();
            MakeHouseAgeMap();


            void MakeHouseAgeMap()
            {
                var minHouseAge = houses.Where(x => x.AverageBuildingAge > 0).Min(x => x.AverageBuildingAge);
                var maxHouseAge = houses.Max(x => x.AverageBuildingAge);
                var range = maxHouseAge - minHouseAge;

                RGB GetColor(House h)
                {
                    if (h.AverageBuildingAge < 1) {
                        return new RGB(0, 0, 128);
                    }

                    var relativeAge = (h.AverageBuildingAge - minHouseAge) / range;
                    var color = (int)(250.0 * relativeAge);
                    return new RGB(color, 0, 0);
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("BuildingAgeMap.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Gebäudealter = " + minHouseAge.ToString("F0"), 0, 0, 0),
                    new MapLegendEntry("Gebäudealter = " + maxHouseAge.ToString("F0"), 255, 0, 0)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }


            void AgeHistogram()
            {
                var filename = MakeAndRegisterFullFilename("AgeHistogram.png", slice);
                var ages = houses.Select(x => x.AverageBuildingAge).Where(y => y > 0).ToList();
                var barSeries = new List<BarSeriesEntry>();
                var h = new Histogram(ages, 100);
                barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(h, out var colnames));
                Services.PlotMaker.MakeBarChart(filename, "AgeHistogram", barSeries, colnames);
            }


            void MakeHouseGebäudeResultsKey()
            {
                var ssa = new SingleSankeyArrow("HouseSankeyMitGebäudeObjektIDs", 1000,
                    MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Complexes", houses.Count, 5000, Orientation.Straight));
                var housesMitGebäude = houses.Where(x => x.GebäudeObjectIDs.Count > 0).ToList();
                var countHousesMitGebäude = housesMitGebäude.Count;
                var housesTotal = houses.Count;
                ssa.AddEntry(new SankeyEntry("Gebäude mit Objektids", countHousesMitGebäude * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Gebäude ohne Objektids", (housesTotal - countHousesMitGebäude) * -1, 5000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }
        }

        private void MakeOtherHouseChart([NotNull] [ItemNotNull] List<PotentialHousehold> allHouseholds, [NotNull] [ItemNotNull] List<GwrData> gwr, [NotNull] [ItemNotNull] List<BuildingComplex> complexes,
                                         [NotNull] [ItemNotNull] List<House> houses, [NotNull] ScenarioSliceParameters slice)
        {
            var points = new List<MapPoint>();
            foreach (var house in houses) {
                if (house.WgsGwrCoords.Count == 0) {
                    continue;
                }

                var complex = complexes.Single(x => x.ComplexGuid == house.ComplexGuid);

                var gwrs = new List<GwrData>();
                foreach (var egid in complex.EGids) {
                    gwrs.AddRange(gwr.Where(x => x.EidgGebaeudeidentifikator_EGID == egid));
                }

                var totalAppartments = gwrs.Select(x => x.AnzahlWohnungen_GANZWHG).Sum();
                var hhCount = allHouseholds.Count(x => x.HouseGuid == house.Guid);
                var diffCount = totalAppartments - hhCount;

                if (diffCount < 0) {
                    points.Add(new MapPoint(house.WgsGwrCoords[0].Lat, house.WgsGwrCoords[0].Lon, 10, 255, 0, 0));
                }
                else if (diffCount > 0) {
                    points.Add(new MapPoint(house.WgsGwrCoords[0].Lat, house.WgsGwrCoords[0].Lon, 10, 0, 255, 0));
                }
                else {
                    points.Add(new MapPoint(house.WgsGwrCoords[0].Lat, house.WgsGwrCoords[0].Lon, 10, 0, 0, 255));
                }
            }
            //const string sectionDescription = "Zeigt die Verteilung der Wohnungen vom GWR vs. Loclanet";
            string fullname = MakeAndRegisterFullFilename("Localnet_validation_houses.svg", slice);
            Services.PlotMaker.MakeMapDrawer(fullname, "Localnet_validation_houses", points, new List<MapLegendEntry>());
            Trace("Testmap written");
        }
        private void MakeHouseKey([NotNull] [ItemNotNull] List<House> houses, [NotNull] [ItemNotNull] List<BuildingComplex> complexes, [NotNull] ScenarioSliceParameters slice)
        {
            var ssa = new SingleSankeyArrow("HouseSankey", 1000, MyStage,
                SequenceNumber, Name, slice, Services);
            ssa.AddEntry(new SankeyEntry("Complexes", complexes.Count, 5000, Orientation.Straight));
            ssa.AddEntry(new SankeyEntry("Houses", houses.Count * -1, 5000, Orientation.Straight));
            ssa.AddEntry(new SankeyEntry("Ignored", (complexes.Count - houses.Count) * -1, 5000, Orientation.Straight));
            Services.PlotMaker.MakeSankeyChart(ssa);
        }

    }
}