using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Visualizer;
using Visualizer.OSM;
using Visualizer.Sankey;
using ServiceRepository = FutureLoadAnalyzerLib.Tooling.ServiceRepository;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public enum HouseType {
        SingleFamiliyHouse,
        MultiFamiliyHouseResidentialOnly,
        MultiFamilityHouseMixedWithBusiness,
        Business,
        Industry,
        Other
    }

    // ReSharper disable once InconsistentNaming
    public class Z_DetermineHousetype : RunableWithBenchmark {
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
            //var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var ap = new AnalysisRepository(Services.RunningConfig);
            var dbHouse = ap.GetSlice(Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var houseTypeEntries = dbHouse.Fetch<HouseTypeEntry>();
            MakeHouseTypeEntries();
            MakeHouseTypeMap();
            MakeEnergyUseXls();
            void MakeEnergyUseXls()
            {
                var households = dbHouse.Fetch<Household>();
                var businesses = dbHouse.Fetch<BusinessEntry>();
                var infra = dbHouse.Fetch<BuildingInfrastructure>();
                var light = dbHouse.Fetch<StreetLightingEntry>();
                var dhw = dbHouse.Fetch<DHWHeaterEntry>();
                var rc = new RowCollection("energy","energy");
                RowBuilder rb = RowBuilder.Start("Haushalte",households.Sum(x=> x.EffectiveEnergyDemand)/Constants.GWhFactor);
                var bigcustomers = businesses.Where(x => x.EffectiveEnergyDemand > 20000);
                var small    = businesses.Where(x => x.EffectiveEnergyDemand <= 20000);
                rb.Add("Geschäftskunden > 20 MWh", bigcustomers.Sum(x => x.EffectiveEnergyDemand) / Constants.GWhFactor);
                rb.Add("Geschäftskunden < 20 MWh", small.Sum(x => x.EffectiveEnergyDemand) / Constants.GWhFactor);
                rb.Add("Gebäudeinfrastruktur", infra.Sum(x => x.EffectiveEnergyDemand) / Constants.GWhFactor);
                rb.Add("Strassenbeleuchtung", light.Sum(x => x.YearlyElectricityUse) / Constants.GWhFactor);
                rb.Add("Elektroboiler", dhw.Sum(x => x.EffectiveEnergyDemand) / Constants.GWhFactor);

                rc.Add(rb);
                var fn = MakeAndRegisterFullFilename("EnergyTreeMap.xlsx", Constants.PresentSlice);
                XlsxDumper.WriteToXlsx(fn,rc);
                SaveToPublicationDirectory(fn,Constants.PresentSlice,"4.2");

            }

            void MakeHouseTypeEntries()
            {
                var ssa = new SingleSankeyArrow("HouseTypeEntries", 1500, MyStage,
                    SequenceNumber, Name,  slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var countsPerType = new Dictionary<HouseType, int>();
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

            void MakeHouseTypeMap()
            {
                var rgbs = new Dictionary<HouseType, RGB>();
                var hs = houseTypeEntries.Select(x => x.HouseType).Distinct().ToList();
                var idx = 0;
                foreach (var type in hs) {
                    var rgb = ColorGenerator.GetRGB(idx++);
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
                    var hse = houseTypeEntries.Single(x => x.HouseGuid == h.Guid);
                    return rgbs[hse.HouseType];
                }

                var mapPoints = houses.Select(x => x.GetMapColorForHouse(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("HouseTypeMap.png", slice);
                var legendEntries = new List<MapLegendEntry>();
                foreach (var pair in rgbs) {
                    legendEntries.Add(new MapLegendEntry(pair.Key.ToString(), pair.Value));
                }

                Services.PlotMaker.MakeOsmMap(Name, filename, mapPoints, new List<WgsPoint>(), legendEntries, new List<LineEntry>());
            }
        }

        protected override void RunActualProcess()
        {
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<HouseTypeEntry>();
            var houses = dbHouses.Fetch<House>();
            var households = dbHouses.Fetch<Household>();
            var businesses = dbHouses.Fetch<BusinessEntry>();
            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                var pointsPerType = new Dictionary<HouseType, double>();
                foreach (HouseType housetype in Enum.GetValues(typeof(HouseType))) {
                    pointsPerType.Add(housetype, 0);
                }

                //houses
                var hhs = households.Where(x => x.HouseGuid == house.Guid).ToList();
                if (hhs.Count == 1) {
                    pointsPerType[HouseType.SingleFamiliyHouse]++;
                }

                if (hhs.Count > 1) {
                    pointsPerType[HouseType.MultiFamiliyHouseResidentialOnly]++;
                    pointsPerType[HouseType.MultiFamilityHouseMixedWithBusiness] += 0.9;
                }

                //businesses
                var business = businesses.Where(x => x.HouseGuid == house.Guid).ToList();
                if (business.Count > 1) {
                    pointsPerType[HouseType.Business]++;
                    pointsPerType[HouseType.MultiFamilityHouseMixedWithBusiness] += 0.9;
                }

                if (Math.Abs(pointsPerType.Values.Sum()) < 0.00001) {
                    pointsPerType[HouseType.Other]++;
                }

                var max = double.MinValue;
                var myenum = HouseType.Other;
                foreach (var pair in pointsPerType) {
                    if (pair.Value > max) {
                        myenum = pair.Key;
                        max = pair.Value;
                    }
                }

                var valueDictionary = JsonConvert.SerializeObject(pointsPerType, Formatting.Indented);
                var hte = new HouseTypeEntry(house.Guid, myenum, valueDictionary, Guid.NewGuid().ToString());
                dbHouses.Save(hte);
            }

            dbHouses.CompleteTransaction();
        }
    }
}