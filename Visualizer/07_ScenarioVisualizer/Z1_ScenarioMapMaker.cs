using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Database;
using Common.ResultFiles;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace BurgdorfStatistics._07_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    public class Z1_ScenarioMapMaker : RunnableForScenarioWithBenchmark
    {
        public Z1_ScenarioMapMaker([NotNull] ServiceRepository services)
            : base(nameof(Z1_ScenarioMapMaker), Stage.ScenarioVisualisation, 2600, services, false)
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


        private void RunOneYear([NotNull] ScenarioSliceParameters up)
        {
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, up);
            var houses = dbHouses.Database.Fetch<House>();
            //   MakeHeatingDensityMap(up,  houses,dbHouses);

            //MakeHeatingSystemMap(up,  dbHouses, houses );
            //MakeCarMaps(up,  dbHouses, houses);
            //PV
            {
                MakePVMap(up, dbHouses, houses);
            }

            //show percentages of max pv
        }
        /*
        private void MakeHeatingDensityMap([NotNull] ScenarioSliceParameters up,[ItemNotNull] [NotNull] List<House> houses,
                                           [NotNull] MyDb dbHouses )
        {
            const string section = "Heizungseffizienz [kWh/m2/a]";
            const string sectionDescription = "Heizungseffizenz über alle Gebäude";
//heating energy density
            //var heatingMethods = dbHouses.Fetch<HouseHeating>();
                var heatingEnergyPoints = new List<MapPoint>();
                double adjustmentFactor = houses.Max(x => x.MergedTotalHeatingEnergyDemand)/100;
                foreach (var house in houses) {
                    if (house.GeoCoords.Count == 0) {
                        continue;
                    }

                    //var hm = heatingMethods.FirstOrDefault(x => x.HouseGuid == house.HouseGuid);

                    var co = house.GeoCoords[0];
                    var adjustedRadius = (int)(house.MergedTotalHeatingEnergyDemand / adjustmentFactor);
                    heatingEnergyPoints.Add(new MapPoint(co.X, co.Y, house.MergedHeatingEnergyIntensity, adjustedRadius));
                }

                string dstFileName2 = dbHouses.GetFullFilename(up.GetFileName() + "_HeatDensity_Map.svg",SequenceNumber,Name);
                ResultFileEntry rfe2 = new ResultFileEntry(section, sectionDescription, dstFileName2.Replace(".svg", ".png"),
                    "", "", up.DstScenario, up.DstYear, Stage.PresentVisualisation);
                rfe2.Save();
            Services.MapDrawer.DrawMapSvg(heatingEnergyPoints, dstFileName2, new List<MapLegendEntry>());
                Log(MessageType.Info, "Heat Density Maps written");
        }*/
        /*
        private void MakeCarMaps([NotNull] ScenarioSliceParameters up, [NotNull] MyDb dbHouses, [ItemNotNull] [NotNull] List<House> houses)
        {
            var cars = dbHouses.Database.Fetch<Car>();
            if (cars.Count == 0)
            {
                return;
            }

            var carPoints = new List<MapPoint>();
            foreach (House house in houses)
            {
                if (house.GeoCoords.Count == 0)
                {
                    continue;
                }

                var carsAtHouse = cars.Where(x => x.HouseGuid == house.HouseGuid).ToList();

                bool hasElectric = carsAtHouse.Any(x => x.CarType == CarType.Electric);
                var co = house.GeoCoords[0];
                    if (carsAtHouse.Count == 0) {
                    carPoints.Add(new MapPoint(co.X, co.Y, 10, 0, 0, 0));
                }
                else {
                        int radius = 10+ carsAtHouse.Count;
                        if (!hasElectric) {
                        carPoints.Add(new MapPoint(co.X, co.Y, radius, 255, 0, 0));
                    }
                    else {
                        carPoints.Add(new MapPoint(co.X, co.Y, radius, 0, 255, 0));
                    }
                }
            }
            if (carPoints.Count == 0) {
                throw new Exception("No points");
            }

            const string section = "Fahrzeuge";
            const string sectionDescription = "Typ und Anzahl von Fahrzeugen";

            string dstFileName2 = dbHouses.GetFullFilename(up.GetFileName() + "_Cars_Map.svg",SequenceNumber,Name);
            ResultFileEntry rfe2 = new ResultFileEntry(section, sectionDescription, dstFileName2.Replace(".svg", ".png"),
                "", "", up.DstScenario, up.DstYear, Stage.PresentVisualisation);
            rfe2.Save();
            Services.MapDrawer.DrawMapSvg(carPoints, dstFileName2, new List<MapLegendEntry>());
            Log(MessageType.Info, "Car Maps written");
        }*/
        /*
        private void MakeHeatingSystemMap([NotNull] ScenarioSliceParameters up, [NotNull] MyDb dbHouses, [ItemNotNull] [NotNull] List<House> houses)
        {
            var heatingSystemEntries = dbHouses.Database.Fetch<HeatingSystemEntry>();
            if (heatingSystemEntries.Count == 0)
            {
                return;
            }

            var pvPoints = new List<MapPoint>();
            foreach (House house in houses) {
                if (house.GeoCoords.Count == 0)
                {
                    continue;
                }
                List<HeatingSystemEntry> hses = new List<HeatingSystemEntry>();
                    var entry = heatingSystemEntries.FirstOrDefault(x => x.HouseGuid == house.HouseGuid);
                    if (entry != null) {
                        hses.Add(entry);
                    }

                    if(hses.Count == 0) {
                    continue;
                }

                bool hasHP = hses.Any(x => x.HeatingSystemType == HeatingSystemType.Heatpump);
                bool hasGas = hses.Any(x => x.HeatingSystemType == HeatingSystemType.Gasheating);
                bool hasOil = hses.Any(x => x.HeatingSystemType == HeatingSystemType.OilHeating);
                
                var co = house.GeoCoords[0];
                if(hasHP) {
                    pvPoints.Add(new MapPoint(co.X, co.Y,  10,255,0,0));
                }
                else if(hasGas) {
                    pvPoints.Add(new MapPoint(co.X, co.Y, 10, 0, 255, 0));
                }
                else if (hasOil) {
                    pvPoints.Add(new MapPoint(co.X, co.Y, 10, 0, 0, 255));
                }
                else {
                    pvPoints.Add(new MapPoint(co.X, co.Y, 10, 0, 0, 0));
                }

            }
            if(pvPoints.Count == 0) {
                throw new Exception("No points");
            }

            const string section = "Heizungssysteme";
            const string sectionDescription = "Art des Heizungssystems";
            string dstFileName2 = dbHouses.GetFullFilename( up.GetFileName() + "_HeatingSystem_Map.svg",SequenceNumber,Name);
            ResultFileEntry rfe2 = new ResultFileEntry(section, sectionDescription, dstFileName2.Replace(".svg", ".png"),
                "", "", up.DstScenario, up.DstYear, Stage.PresentVisualisation);
            rfe2.Save();
            Services.MapDrawer.DrawMapSvg(pvPoints, dstFileName2, new List<MapLegendEntry>());
            Log(MessageType.Info, "PV Maps written");
        }*/

        private void MakePVMap([NotNull] ScenarioSliceParameters parameters, [NotNull] MyDb dbHouses, [ItemNotNull] [NotNull] List<House> houses)
        {
            var pvSystems = dbHouses.Database.Fetch<PvSystemEntry>();
            if (pvSystems.Count == 0) {
                return;
            }

            var adjustmentfactor = pvSystems.Max(x => x.YearlyPotential) / 100;
            var pvPoints = new List<MapPoint>();
            foreach (var house in houses) {
                if (house.WgsGwrCoords.Count == 0) {
                    continue;
                }

                var co = house.WgsGwrCoords[0];
                var pvSystem = pvSystems.FirstOrDefault(x => x.HouseGuid == house.HouseGuid);
                if (pvSystem != null) {
                    var radius = (int)(pvSystem.YearlyPotential / adjustmentfactor);
                    if (radius < 10) {
                        radius = 10;
                    }

                    pvPoints.Add(new MapPoint(co.Lat, co.Lon, radius, 255, 0, 0));
                }
                else {
                    pvPoints.Add(new MapPoint(co.Lat, co.Lon, 10, 0, 0, 0));
                }
            }

            const string section = "Photovoltaik";
            const string sectionDescription = "Leistung der PV-Systeme";
            var dstFileName2 = dbHouses.GetFullFilename(parameters.GetFileName() + "_PV_power_Map.svg", SequenceNumber, Name);
            var rfe2 = new ResultFileEntry(section, sectionDescription,
                dstFileName2.Replace(".svg", ".png"), "", "",
                parameters, MyStage);
            rfe2.Save();
            var lge = new List<MapLegendEntry> {
                new MapLegendEntry(parameters.DstScenario.ToString() + " Jahr " + parameters.DstYear.ToString(), Constants.Black)
            };
            Services.MapDrawer.DrawMapSvg(pvPoints, dstFileName2, lge,MyStage);
            Log(MessageType.Info, "PV Maps written");
        }


        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> parameters)
        {
            foreach (ScenarioSliceParameters parameter in parameters) {
                RunOneYear(parameter);
            }
        }
    }
}