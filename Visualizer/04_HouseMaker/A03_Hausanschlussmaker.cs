using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using OfficeOpenXml;
using Visualizer.OSM;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A03_HouseAnschlussmaker : RunableWithBenchmark {
        public A03_HouseAnschlussmaker([NotNull] ServiceRepository services)
            : base(nameof(A03_HouseAnschlussmaker), Stage.Houses, 3, services, true)
        {
            DevelopmentStatus.Add("Make a map that has each orignal hausanschluss");
            DevelopmentStatus.Add("Make a map that has each orignal hausanschluss and each not assigned one in a different color and large");
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<Hausanschluss>(Stage.Houses, Constants.PresentSlice);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var isnLocations = dbRaw.Fetch<HausanschlussImport>();
            var houses = dbHouse.Fetch<House>();
            //assign netzanschlüsse
            var assignedHouses = new List<House>();
            var hausanschlusses = new List<Hausanschluss>();
            dbHouse.BeginTransaction();
            foreach (House house in houses) {
                house.Hausanschluss.Clear();
            }
            DoDirectAssignments(isnLocations, houses,  hausanschlusses, assignedHouses);

            Info("Hausanschlüsse 1: " + hausanschlusses.Count);
            var assignedHausanschlüsse1 = houses.SelectMany(x => x.Hausanschluss).ToList();
            Info("Assigned Hausanschlüsse: " + assignedHausanschlüsse1.Count);
            DoProximityAssignments(houses, assignedHouses, isnLocations, hausanschlusses);
            Info("Hausanschlüsse 2: " +hausanschlusses.Count);
            var assignedHausanschlüsse2 = houses.SelectMany(x => x.Hausanschluss).ToList();
            Info("Assigned Hausanschlüsse: " + assignedHausanschlüsse2.Count);
            foreach (var hausanschluss in hausanschlusses) {
                dbHouse.Save(hausanschluss);
                var house = houses.Single(x => x.HouseGuid == hausanschluss.HouseGuid);
                if(!house.Hausanschluss.Any(x=> x.HausanschlussGuid == hausanschluss.HausanschlussGuid)) {
                    house.Hausanschluss.Add(hausanschluss);
                }

                if (house.Hausanschluss.Select(x=> x.HausanschlussGuid).Distinct().Count() != house.Hausanschluss.Count) {
                    throw new FlaException("Duplicate hausanschluss-entries");
                }
            }

            var assignedHausanschlüsse = houses.SelectMany(x => x.Hausanschluss).ToList();
            Info("Assigned Hausanschlüsse: " + assignedHausanschlüsse.Count);
            foreach (House house in houses) {
                dbHouse.Save(house);
            }

            List<House> housesWithoutAnschluss = houses.Where(x => x.Hausanschluss.Count == 0).ToList();
            foreach (var house in housesWithoutAnschluss) {
                string s = house.ComplexName;
                if (house.ComplexName.EndsWith("a")) {
                    var parentHouse = houses.Where(x => house.ComplexName.Contains(x.ComplexName) && x != house).Select(x=> x.ComplexName).ToList();
                    if (parentHouse.Count > 0) {
                        s +=  ";" + string.Join(",", parentHouse);
                        Info(s);
                    }
                }
                Info(s);
            }
            if(housesWithoutAnschluss.Count > 0) {
                throw new Exception("House without Hausanschluss total: " + housesWithoutAnschluss.Count);
            }

            foreach (House house in houses) {
                if (house.Hausanschluss.Count == 0) {
                    throw new FlaException("No Hausanschluss");
                }
            }
            dbHouse.CompleteTransaction();
        }
        private static void DoProximityAssignments([NotNull] [ItemNotNull] List<House> houses, [NotNull] [ItemNotNull] List<House> assignedHouses, [NotNull] [ItemNotNull] List<HausanschlussImport> isnLocations,
                                                   [NotNull][ItemNotNull] List<Hausanschluss> hausanschlusses)
        {
            foreach (var assignedHouse in assignedHouses) {
                //check if every assigned house is actually assigned.
                var r = hausanschlusses.FirstOrDefault(x => x.HouseGuid == assignedHouse.HouseGuid);
                if (r == null) {
                    throw new FlaException("Assigned house is not actually assigned.");
                }
            }
            foreach (var house in houses)
            {
                if (!assignedHouses.Contains(house)) {
                    var locations = new List<Tuple<double, HausanschlussImport>>();
                    var houseWgsPoints = new List<WgsPoint>();
                    houseWgsPoints.AddRange(house.LocalWgsPoints);
                    if (houseWgsPoints.Count == 0) {
                        houseWgsPoints.AddRange(house.WgsGwrCoords);
                    }

                    if (houseWgsPoints.Count > 0) {
                        foreach (var isnLocation in isnLocations) {
                            var wgsp = new WgsPoint(isnLocation.Lon, isnLocation.Lat);
                            var distance = wgsp.GetMinimumDistance(houseWgsPoints);
                            locations.Add(new Tuple<double, HausanschlussImport>(distance, isnLocation));
                        }


                        locations.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                        var pickedHa = locations[0].Item2;
                        //if (locations[0].Item1 > 200) {
                            //throw new Exception("More than 200 m");
                        //}

                        hausanschlusses.Add(new Hausanschluss(Guid.NewGuid().ToString(), house.HouseGuid,
                            pickedHa.ObjectID, pickedHa.Egid, pickedHa.Isn, pickedHa.Lon, pickedHa.Lat,
                            pickedHa.Trafokreis, HouseMatchingType.Proximity, locations[0].Item1,locations[0].Item2.Adress));
                    }
                }
            }
        }

        private  void DoDirectAssignments([NotNull] [ItemNotNull] List<HausanschlussImport> isnLocations, [NotNull] [ItemNotNull] List<House> houses,
                                          [NotNull] [ItemNotNull] List<Hausanschluss> hausAnschlusses, [NotNull] [ItemNotNull] List<House> assignedHouses)
        {
            Info("#######################");
            Info("Starting Direct assignments:");
            Info("Isn Locations: " + isnLocations.Count);
            Info("Houses:" + houses.Count);
            Info("Hausanschlüsse:" + hausAnschlusses.Count);
            Info("assignedHouses:" + assignedHouses.Count);

            foreach (var isn in isnLocations)
            {
                House h = null;

                //isn
                if (isn.Isn > 0)
                {
                    var housesIsn = houses.Where(x => x.GebäudeObjectIDs.Contains(isn.Isn)).ToList();
                    if (housesIsn.Count == 1)
                    {
                        h = housesIsn[0];
                    }
                }

                //egid
                if (isn.Egid > 0)
                {
                    var housesEgid = houses.Where(x => x.EGIDs.Contains((int)isn.Egid)).ToList();
                    if (housesEgid.Count == 1)
                    {
                        h = housesEgid[0];
                    }

                    if (h != null)
                    {
                        assignedHouses.Add(h);
                        hausAnschlusses.Add(new Hausanschluss(Guid.NewGuid().ToString(),
                            h.HouseGuid,
                            isn.ObjectID,isn.Egid,
                            isn.Isn,
                            isn.Lon,
                            isn.Lat,
                            isn.Trafokreis,
                            HouseMatchingType.Direct,
                            0,isn.Adress));
                    }
                }
            }
            Info("Finished Direct assignments:");
            Info("Isn Locations: " + isnLocations.Count);
            Info("Houses:" + houses.Count);
            Info("Hausanschlüsse:" + hausAnschlusses.Count);
            Info("assignedHouses:" + assignedHouses.Count);
            Info("#######################");
        }
        protected override void RunChartMaking()
        {
            //var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //var isnLocations = dbRaw.Fetch<HausanschlussImport>();
            var houses = dbHouse.Fetch<House>();
            var houseanschlusses = dbHouse.Fetch<Hausanschluss>();
            ExportAsExcel(houseanschlusses, houses);

            RGB HouseFunc(House h)
            {
                var ha = houseanschlusses.FirstOrDefault(x => x.HouseGuid == h.HouseGuid);
                if(ha?.MatchingType == HouseMatchingType.Direct )
                {
                    return Constants.Blue;
                }

                if (ha?.MatchingType == HouseMatchingType.Proximity) {
                    return Constants.Green;
                }
                return Constants.Orange;
            }

            var houseEntries = houses.Select(x => x.GetMapColorForHouse(HouseFunc)).ToList();
            var filename = MakeAndRegisterFullFilename("DirektZugeordnete.png", Name, "", Constants.PresentSlice);
            var labels = new List<MapLegendEntry> {
                new MapLegendEntry("Direkt Gemappt", Constants.Blue),
                new MapLegendEntry("Näherung", Constants.Green),
                new MapLegendEntry("Kein Hausobjekt hinterlegt", Constants.Orange),
                new MapLegendEntry("Nicht Teil von Burgdorf", Constants.Red)
            };
            var lines = new List<LineEntry>();

            foreach (var hausanschluss in houseanschlusses)
            {
                if (hausanschluss.MatchingType == HouseMatchingType.Direct)
                {
                    continue;
                }

                var house = houses.Single(x => x.HouseGuid == hausanschluss.HouseGuid);
                foreach (WgsPoint coord in house.WgsGwrCoords)
                {

                    WgsPoint isn = new WgsPoint(hausanschluss.Lon, hausanschluss.Lat);
                    //house.ComplexName + " - " + hausanschluss.ObjectID
                    lines.Add(new LineEntry(coord, isn,""));
                }

            }
            Services.PlotMaker.MakeOsmMap(Name, filename, houseEntries, new List<WgsPoint>(), labels, lines);
        }

        private void ExportAsExcel([NotNull][ItemNotNull] List<Hausanschluss> hausanschlusses, [NotNull] [ItemNotNull] List<House> houses)
        {
            using (var p = new ExcelPackage()) {
                //A workbook must have at least on cell, so lets add one...
                var ws = p.Workbook.Worksheets.Add("Alle Hausanschluesse");
                //To set values in the spreadsheet use the Cells indexer.
                //header
                ws.Cells[1, 1].Value = "ObjectID";
                ws.Cells[1, 2].Value = "EGID";
                ws.Cells[1, 3].Value = "ISN";
                ws.Cells[1, 4].Value = "Lon";
                ws.Cells[1, 5].Value = "Lat";
                ws.Cells[1, 6].Value = "HouseIds";
                ws.Cells[1, 7].Value = "Gesamter Stromverbrauch";
                Dictionary<string, House> housesByHaGuid = new Dictionary<string, House>();
                foreach (House house in houses) {
                    foreach (Hausanschluss hausanschluss in house.Hausanschluss) {
                        housesByHaGuid.Add(hausanschluss.HausanschlussGuid,house);
                    }
                }
                var row = 2;
                foreach (var hausanschluss in hausanschlusses) {
                    ws.Cells[row, 1].Value = hausanschluss.ObjectID;
                    ws.Cells[row, 2].Value = hausanschluss.Egid;
                    ws.Cells[row, 3].Value = hausanschluss.Isn;
                    ws.Cells[row, 4].Value = hausanschluss.Lon;
                    ws.Cells[row, 5].Value = hausanschluss.Lat;
                    House house = housesByHaGuid[hausanschluss.HausanschlussGuid];
                    //if (matchedHouses.Count == 0) {
                        //throw new FlaException("Houseanschluss without house.");
                    //}
                        //var e = house.CollectTotalEnergyConsumptionFromLocalnet(energyUses);
                        //electricitySum += e.ElectricityUse;
                        //gasSum += e.GasUse;

                    ws.Cells[row, 6].Value = house.ComplexName;
                    ws.Cells[row, 7].Value = hausanschluss.MatchingType.ToString();
                    ws.Cells[row, 8].Value = hausanschluss.Distance;
                    //ws.Cells[row, 7].Value = electricitySum;
                    //ws.Cells[row, 8].Value = gasSum;
                    row++;
                }


                var ws2 = p.Workbook.Worksheets.Add("Alle Häuser");
                ws2.Cells[1, 1].Value = "Adresse";
                ws2.Cells[1, 2].Value = "EGids";
                ws2.Cells[1, 3].Value = "IsnIds";
                ws2.Cells[1, 4].Value = "Stromverbrauch";
                ws2.Cells[1, 5].Value = "Gasverbrauch";
                ws2.Cells[1, 6].Value = "ISN Ids";
                row = 2;
                foreach (var house in houses) {
                    //var houseSum = house.CollectTotalEnergyConsumptionFromLocalnet(energyUses);
                    ws2.Cells[row, 1].Value = house.ComplexName;
                    ws2.Cells[row, 2].Value = house.EGIDsAsJson;
                    ws2.Cells[row, 3].Value = house.GebäudeIDsAsJson;
                    //ws2.Cells[row, 4].Value = houseSum.ElectricityUse;
                    //ws2.Cells[row, 5].Value = houseSum.GasUse;
                    ws2.Cells[row, 4].Value = house.HausanschlussAsJson;
                    //var matchedIsn = hausanschlusses.Where(x => x.House == house).ToList();
                    //var ids = string.Join(",", matchedIsn.Select(x => x.Isn.ObjectID + " (" + x.MatchingType.ToString() + ", " + x.Distance.ToString("F0") + ")"));
                    //ws2.Cells[row, 6].Value = ids;
                    row++;
                }
                var ws3 = p.Workbook.Worksheets.Add("Statistik");
                row = 1;
                ws3.Cells[row, 1].Value = "Anzahl Gebäude insgesamt";
                ws3.Cells[row++, 2].Value = houses.Count;
                ws3.Cells[row, 1].Value = "Anzahl Hausanschlüsse";
                ws3.Cells[row++, 2].Value = hausanschlusses.Count;
                ws3.Cells[row, 1].Value = "Anzahl häuser mit keinem Anschluss";
                ws3.Cells[row++, 2].Value = houses.Where(x => x.Hausanschluss.Count == 0).Count();
                ws3.Cells[row, 1].Value = "Anzahl häuser mit genau einem Anschluss";
                ws3.Cells[row++, 2].Value = houses.Where(x => x.Hausanschluss.Count == 1).Count();
                ws3.Cells[row, 1].Value = "Anzahl häuser mit genau zwei Anschluss";
                ws3.Cells[row++, 2].Value = houses.Where(x => x.Hausanschluss.Count == 2).Count();
                ws3.Cells[row, 1].Value = "Anzahl häuser mit mehr Anschlüssen";
                ws3.Cells[row++, 2].Value = houses.Where(x => x.Hausanschluss.Count > 2).Count();
                var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
                var isnLocations = dbRaw.Fetch<HausanschlussImport>();
                ws3.Cells[row, 1].Value = "Anzahl Ursprünglicher Hausanschlussids";
                ws3.Cells[row++, 2].Value = isnLocations.Count;
                ws3.Cells[row, 1].Value = "Anzahl eindeutiger ursprünglicher Hausanschlussids";
                ws3.Cells[row++, 2].Value = isnLocations.Select(x=> x.ObjectID).Distinct().Count();
                var allezugewiesenenHausanschlüsse = houses.SelectMany(x => x.Hausanschluss).ToList();
                ws3.Cells[row, 1].Value = "Anzahl eindeutiger zugewiesener Hausanschlussids";
                ws3.Cells[row++, 2].Value = allezugewiesenenHausanschlüsse.Select(x => x.ObjectID).Distinct().Count();

                var alleZugewiesenenIsns = allezugewiesenenHausanschlüsse.Select(x => x.Isn).Distinct().ToList();
                ws3.Cells[row, 1].Value = "Anzahl zugewiesener ISNs";
                ws3.Cells[row, 2].Value = alleZugewiesenenIsns.Count();


                var xfilename = MakeAndRegisterFullFilename("Export.xlsx", Name, "", Constants.PresentSlice);
                //Save the new workbook. We haven't specified the filename so use the Save as method.
                p.SaveAs(new FileInfo(xfilename));
            }
        }
    }
}