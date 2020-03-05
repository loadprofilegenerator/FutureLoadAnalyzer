using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using ServiceRepository = BurgdorfStatistics.Tooling.ServiceRepository;

namespace BurgdorfStatistics._04_HouseMaker {
    public enum HouseTypeEnum {
        SingleFamiliyHouse,
        MultiFamiliyHouseResidentialOnly,
        MultiFamilityHouseMixedWithBusiness,
        Business,
        Industry,
        Other
    }

    public class HouseTypeEntry {
        public HouseTypeEntry([NotNull] string houseGuid, HouseTypeEnum houseType, [NotNull] string valueDictionary)
        {
            HouseGuid = houseGuid;
            HouseType = houseType;
            ValueDictionary = valueDictionary;
        }

        public int ID { get; set; }
        [NotNull]
        public string HouseGuid { get; set; }
        public HouseTypeEnum HouseType { get; set; }
        [NotNull]
        public string ValueDictionary { get; set; }
    }

    // ReSharper disable once InconsistentNaming
    internal class Z_DetermineHousetype : RunableWithBenchmark {
        public Z_DetermineHousetype([NotNull] ServiceRepository services)
            : base(nameof(Z_DetermineHousetype), Stage.Houses, 2600, services, false)
        {
            DevelopmentStatus.Add("//todo: figure out the house type");
            DevelopmentStatus.Add("//efh / mfh / gewerbe / industrie /wwk sonstiges / garage /?");
            DevelopmentStatus.Add("//gwr + feuerungsstättendaten");
            DevelopmentStatus.Add("//todo: check if there are really this few business-only buildings");
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var houseTypeEntries = dbHouse.Fetch<HouseTypeEntry>();
            MakeHouseTypeEntries();
            //HeatingSystemCountHistogram();
            //MakeHeatingSystemMapError();
            //MakeHeatingSystemMap();
            MakeHouseTypeMap();

            void MakeHouseTypeEntries()
            {
                var ssa = new SingleSankeyArrow("HouseTypeEntries", 1500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var countsPerType = new Dictionary<HouseTypeEnum, int>();
                foreach (var entry in houseTypeEntries) {
                    if (!countsPerType.ContainsKey(entry.HouseType)) {
                        countsPerType.Add(entry.HouseType, 0);
                    }

                    countsPerType[entry.HouseType]++;
                }

                foreach (var pair in countsPerType) {
                    ssa.AddEntry(new SankeyEntry(pair.Key.ToString(), pair.Value * -1, 2000, Orientation.Up));
                }

                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            /*
            void MakeHeatingSystemMapError()
            {
                RGB GetColor(House h)
                {
                    HeatingSystemEntry hse = heatingsystems.Single(x => x.HouseGuid == h.HouseGuid);
                    if (hse.HeatingSystemType == HeatingSystemType.Fernwärme)
                    {
                        return Constants.Red;
                    }
                    if (hse.HeatingSystemType == HeatingSystemType.Gasheating)
                    {
                        return Constants.Orange;
                    }
                    if (hse.HeatingSystemType == HeatingSystemType.FeuerungsstättenGas)
                    {
                        return Constants.Orange;
                    }
                    return Constants.Black;
                }

                var mapPoints = houses.Select(x => x.GetMapPoint(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("KantonHeatingSystemErrors.svg", Name, "");
                List<MapLegendEntry> legendEntries = new List<MapLegendEntry>();
                legendEntries.Add(new MapLegendEntry("Kanton Fernwärme", Constants.Red));
                legendEntries.Add(new MapLegendEntry("Kanton Gas", Constants.Orange));
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }*/
            void MakeHouseTypeMap()
            {
                var rgbs = new Dictionary<HouseTypeEnum, RGB>();
                var hs = houseTypeEntries.Select(x => x.HouseType).Distinct().ToList();
                var cg = new ColorGenerator();
                var idx = 0;
                foreach (var type in hs) {
                    var rgb = cg.GetRGB(idx++);
                    if (rgb.R == 255 && rgb.B == 255 && rgb.G == 255) {
                        rgb = new RGB(200, 200, 200);
                    }

                    if (rgb.R == 255 && rgb.B == 0 && rgb.G == 0) {
                        rgb = Constants.Orange;
                    }

                    rgbs.Add(type, rgb);
                }

                RGB GetColor(House h)
                {
                    var hse = houseTypeEntries.Single(x => x.HouseGuid == h.HouseGuid);
                    return rgbs[hse.HouseType];
                }

                var mapPoints = houses.Select(x => x.GetMapColorForHouse(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("HouseTypeMap.png", Name, "", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in rgbs) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeOsmMap(Name, filename, mapPoints, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<HouseTypeEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouses.Fetch<House>();
            var households = dbHouses.Fetch<Household>();
            var businesses = dbHouses.Fetch<BusinessEntry>();
            //var potentialCookingSystemEntry = dbHouses.Fetch<PotentialCookingSystemEntry>();
            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                var pointsPerType = new Dictionary<HouseTypeEnum, double>();
                foreach (HouseTypeEnum housetype in Enum.GetValues(typeof(HouseTypeEnum))) {
                    pointsPerType.Add(housetype, 0);
                }

                //houses
                var hhs = households.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                if (hhs.Count == 1) {
                    pointsPerType[HouseTypeEnum.SingleFamiliyHouse]++;
                }

                if (hhs.Count > 1) {
                    pointsPerType[HouseTypeEnum.MultiFamiliyHouseResidentialOnly]++;
                    pointsPerType[HouseTypeEnum.MultiFamilityHouseMixedWithBusiness] += 0.9;
                }

                //businesses
                var business = businesses.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                if (business.Count > 1) {
                    pointsPerType[HouseTypeEnum.Business]++;
                    pointsPerType[HouseTypeEnum.MultiFamilityHouseMixedWithBusiness] += 0.9;
                }

                if (Math.Abs(pointsPerType.Values.Sum()) < 0.00001) {
                    pointsPerType[HouseTypeEnum.Other]++;
                }

                var max = double.MinValue;
                var myenum = HouseTypeEnum.Other;
                foreach (var pair in pointsPerType) {
                    if (pair.Value > max) {
                        myenum = pair.Key;
                        max = pair.Value;
                    }
                }

                var valueDictionary = JsonConvert.SerializeObject(pointsPerType, Formatting.Indented);
                var hte = new HouseTypeEntry(house.HouseGuid, myenum, valueDictionary);
                dbHouses.Save(hte);
            }

            dbHouses.CompleteTransaction();
        }
    }
}