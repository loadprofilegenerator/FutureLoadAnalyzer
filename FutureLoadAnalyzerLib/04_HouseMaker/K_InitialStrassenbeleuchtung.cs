/*using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming

    // ReSharper disable once InconsistentNaming
    public class K_InitialStrassenbeleuchtung : RunableWithBenchmark {
        public K_InitialStrassenbeleuchtung([NotNull] ServiceRepository services) : base(nameof(K_InitialStrassenbeleuchtung),
            Stage.Houses,
            1100,
            services,
            false)
        {
            DevelopmentStatus.Add("Make a proper profile");
            DevelopmentStatus.Add("Consider street light entries! Check if they have a isn");
        }

        protected override void RunActualProcess()
        {
            //stassenbeleuchtungen werden von den localnet daten bereits gesetzt, hier nur visualisierung
        }

        protected void RunActualProcess1()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<StreetLight>();
            dbHouses.RecreateTable<LightingProfile>();
            var hausanschlusseImports = dbRaw.Fetch<HausanschlussImport>();
            var hausanschlusse = dbHouses.Fetch<Hausanschluss>();

            var leuchten = hausanschlusseImports.Where(x => x.ObjectID.Contains("LEUCHTE")).ToList();
            var usedHAs = hausanschlusse.Select(x => x.ObjectID).Distinct().ToList();
            var freieLeuchtenAnschluesse = leuchten.Where(x => !usedHAs.Contains(x.ObjectID)).ToList();
            //alte laternen löschen
            var houses = dbHouses.Fetch<House>();
            dbHouses.BeginTransaction();
            var oldLaterns = houses.Where(x => x.ComplexName.StartsWith("Laterne ")).ToList();
            foreach (var house in oldLaterns) {
                dbHouses.Delete(house);
            }

            Info("Leuchten gefunden: " + freieLeuchtenAnschluesse.Count);
            //todo: make a profile
            List<double> values = new List<double> {0};
            var profile = new JsonSerializableProfile("myprofile", values.AsReadOnly(), EnergyOrPower.Energy);
            LightingProfile lightProfile = new LightingProfile("myLightProfile", profile, Guid.NewGuid().ToString());
            dbHouses.Save(lightProfile);
            //todo:
            foreach (var importedHausanschluss in freieLeuchtenAnschluesse) {
                string houseGuid = Guid.NewGuid().ToString();
                House house = new House("Laterne " + importedHausanschluss.ObjectID, houseGuid, houseGuid);
                string haGuid = Guid.NewGuid().ToString();
                Hausanschluss laternenHa = new Hausanschluss(haGuid,
                    houseGuid,
                    importedHausanschluss.ObjectID,
                    0,
                    (int)importedHausanschluss.Egid,
                    importedHausanschluss.Lon,
                    importedHausanschluss.Lat,
                    importedHausanschluss.Trafokreis,
                    HouseMatchingType.DirectByIsn,
                    0,
                    importedHausanschluss.Adress,
                    importedHausanschluss.Standort);
                house.Hausanschluss.Add(laternenHa);
                StreetLight sl = new StreetLight("Laterne " + importedHausanschluss.ObjectID,
                    1000,
                    0,
                    new List<int> {importedHausanschluss.Isn},
                    importedHausanschluss.Isn,
                    houseGuid,
                    haGuid,
                    Guid.NewGuid().ToString(),
                    lightProfile.Guid,
                    100,
                    "fake standort");
                dbHouses.Save(sl);
                dbHouses.Save(laternenHa);
                dbHouses.Save(house);
            }

            dbHouses.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var lights = dbHouse.Fetch<StreetLightingEntry>();
            var lightHouseGuids = lights.Select(x => x.HouseGuid).Distinct().ToList();
            MakeLightSankey();
            MakeCommuterMap();

            void MakeLightSankey()
            {
                var ssa = new SingleSankeyArrow("LightingHouses", 1500, MyStage, SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Houses", houses.Count, 5000, Orientation.Straight));
                var housesWithLight = lightHouseGuids.Count;

                ssa.AddEntry(new SankeyEntry("Strassenbeleuchtungen", housesWithLight * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Sonstige Häuser", (houses.Count - housesWithLight) * -1, 2000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakeCommuterMap()
            {
                RGBWithSize GetColor(House h)
                {
                    if (lightHouseGuids.Contains(h.Guid)) {
                        return new RGBWithSize(Constants.Red, 20);
                    }

                    return new RGBWithSize(Constants.Black, 10);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor, House.CoordsToUse.Localnet)).ToList();

                var filename = MakeAndRegisterFullFilename("Strassenbeleuchtung.svg", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Strassenbeleuchtung", Constants.Red),
                    new MapLegendEntry("Sonstiges", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
            }
        }
    }
}*/