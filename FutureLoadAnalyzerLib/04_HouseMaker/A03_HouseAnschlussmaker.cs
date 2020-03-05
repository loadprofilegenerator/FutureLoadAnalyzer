using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A03_HouseAnschlussmaker : RunableWithBenchmark {
        public A03_HouseAnschlussmaker([NotNull] ServiceRepository services) : base(nameof(A03_HouseAnschlussmaker), Stage.Houses, 3, services, true)
        {
            DevelopmentStatus.Add("Make a map that has each orignal hausanschluss");
            DevelopmentStatus.Add("Make a map that has each orignal hausanschluss and each not assigned one in a different color and large");
            DevelopmentStatus.Add("Fix the deletion of invalid houses in Oberburg somehow");
        }

        protected override void RunActualProcess()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouse.RecreateTable<Hausanschluss>();
            var isnLocations = LoadMergedHausanschlussImports(dbRaw);
            var houses = dbHouse.Fetch<House>();
            //assign netzanschlüsse
            var assignedHouses = new List<House>();
            var hausanschlusses = new List<Hausanschluss>();
            foreach (House house in houses) {
                house.Hausanschluss.Clear();
            }

            DoDirectAssignments(isnLocations, houses, hausanschlusses, assignedHouses);

            Debug("Hausanschlüsse 1: " + hausanschlusses.Count);
            DoProximityAssignments(houses, assignedHouses, isnLocations, hausanschlusses);
            Debug("Hausanschlüsse 2: " + hausanschlusses.Count);
            var assignedHausanschlüsse2 = houses.SelectMany(x => x.Hausanschluss).ToList();
            Debug("Assigned Hausanschlüsse: " + assignedHausanschlüsse2.Count);
            dbHouse.BeginTransaction();
            foreach (var hausanschluss in hausanschlusses) {
                dbHouse.Save(hausanschluss);
                var matchingHouses = houses.Where(x => x.Guid == hausanschluss.HouseGuid);
                foreach (var matchingHouse in matchingHouses) {
                    if (matchingHouse.Hausanschluss.All(x => x.Guid != hausanschluss.Guid)) {
                        matchingHouse.Hausanschluss.Add(hausanschluss);
                    }

                    if (matchingHouse.Hausanschluss.Select(x => x.Guid).Distinct().Count() != matchingHouse.Hausanschluss.Count) {
                        throw new FlaException("Duplicate hausanschluss-entries");
                    }
                }
            }

            var assignedHausanschlüsse = houses.SelectMany(x => x.Hausanschluss).ToList();
            Debug("Assigned Hausanschlüsse: " + assignedHausanschlüsse.Count);
            foreach (House house in houses) {
                dbHouse.Save(house);
            }

            List<House> housesWithoutAnschluss = houses.Where(x => x.Hausanschluss.Count == 0).ToList();
            foreach (House house in housesWithoutAnschluss) {
                dbHouse.Delete(house);
                Debug("Deleted house " + house.ComplexName + " because no hausanschluss could be found");
            }

            dbHouse.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var housesWithoutAnschluss = houses.Where(x => x.Hausanschluss.Count == 0).ToList();
            if (housesWithoutAnschluss.Count > 0) {
                throw new FlaException("Houses without anschluss");
            }

            var houseanschlusses = dbHouse.Fetch<Hausanschluss>();
            ExportAsExcel(houseanschlusses, houses);

            RGB HouseFunc(House h)
            {
                var ha = houseanschlusses.FirstOrDefault(x => x.HouseGuid == h.Guid);
                switch (ha?.MatchingType) {
                    case HouseMatchingType.DirectByEgid:
                    case HouseMatchingType.DirectByAdress:
                    case HouseMatchingType.DirectByIsn:
                    case HouseMatchingType.DirectByIsnSupplemental:
                    case HouseMatchingType.DirectByAdressSupplemental:
                        return Constants.Blue;
                    case HouseMatchingType.Proximity:
                        return Constants.Green;
                    case HouseMatchingType.Leftover:
                        return Constants.Orange;
                    default:
                        return Constants.Orange;
                }
            }

            var houseEntries = houses.Select(x => x.GetMapColorForHouse(HouseFunc)).ToList();
            var filename = MakeAndRegisterFullFilename("DirektZugeordnete.png", Constants.PresentSlice);
            var labels = new List<MapLegendEntry> {
                new MapLegendEntry("Direkt Gemappt", Constants.Blue),
                new MapLegendEntry("Näherung", Constants.Green),
                new MapLegendEntry("Kein Hausobjekt hinterlegt", Constants.Orange),
                new MapLegendEntry("Nicht Teil von Burgdorf", Constants.Red)
            };
            var lines = new List<LineEntry>();

            foreach (var hausanschluss in houseanschlusses) {
                if (hausanschluss.MatchingType == HouseMatchingType.DirectByEgid || hausanschluss.MatchingType == HouseMatchingType.DirectByIsn ||
                    hausanschluss.MatchingType == HouseMatchingType.DirectByAdress) {
                    continue;
                }

                var house = houses.Single(x => x.Guid == hausanschluss.HouseGuid);
                foreach (WgsPoint coord in house.WgsGwrCoords) {
                    WgsPoint isn = new WgsPoint(hausanschluss.Lon, hausanschluss.Lat);
                    //house.ComplexName + " - " + hausanschluss.ObjectID
                    lines.Add(new LineEntry(coord, isn, ""));
                }
            }

            Services.PlotMaker.MakeOsmMap(Name, filename, houseEntries, new List<WgsPoint>(), labels, lines);
        }

        private void DoDirectAssignments([NotNull] [ItemNotNull] List<HausanschlussImport> importedHausanschluss,
                                         [NotNull] [ItemNotNull] IReadOnlyCollection<House> houses,
                                         [NotNull] [ItemNotNull] List<Hausanschluss> hausAnschlusses,
                                         [NotNull] [ItemNotNull] List<House> assignedHouses)
        {
            Debug("#######################");
            Debug("Starting Direct assignments:");
            Debug("Importet Hausanschluss Locations: " + importedHausanschluss.Count);
            Debug("Houses:" + houses.Count);
            Debug("Hausanschlüsse:" + hausAnschlusses.Count);
            Debug("assignedHouses:" + assignedHouses.Count);
            Debug("#######################");
            foreach (var hausanschlussImport in importedHausanschluss) {
                var housesForThisAnschluss = new List<Tuple<House, HouseMatchingType>>();

                //haus für die isn suchen
                if (hausanschlussImport.Isn > 0) {
                    var housesIsn = houses.Where(x => x.GebäudeObjectIDs.Contains(hausanschlussImport.Isn)).ToList();
                    foreach (var house in housesIsn) {
                        var housematching = HouseMatchingType.DirectByIsn;
                        if (!string.IsNullOrWhiteSpace(hausanschlussImport.Standort)) {
                            housematching = HouseMatchingType.DirectByIsnSupplemental;
                        }

                        housesForThisAnschluss.Add(new Tuple<House, HouseMatchingType>(house, housematching));
                    }
                }

                if (!string.IsNullOrWhiteSpace(hausanschlussImport.Adress)) {
                    var houseByAdress = houses.FirstOrDefault(x => x.Adress == hausanschlussImport.Adress);
                    if (houseByAdress != null) {
                        var housematching = HouseMatchingType.DirectByAdress;
                        if (!string.IsNullOrWhiteSpace(hausanschlussImport.Standort)) {
                            housematching = HouseMatchingType.DirectByAdressSupplemental;
                        }

                        housesForThisAnschluss.Add(new Tuple<House, HouseMatchingType>(houseByAdress, housematching));
                    }
                }

                //egid
                if (hausanschlussImport.Egid > 0) {
                    var housesEgid = houses.Where(x => x.EGIDs.Contains((int)hausanschlussImport.Egid)).ToList();
                    foreach (var house in housesEgid) {
                        housesForThisAnschluss.Add(new Tuple<House, HouseMatchingType>(house, HouseMatchingType.DirectByEgid));
                    }
                }

                foreach (var house in housesForThisAnschluss) {
                    if (hausAnschlusses.Any(x =>
                        x.HouseGuid == house.Item1.Guid && x.ObjectID == hausanschlussImport.ObjectID &&
                        x.Standort == hausanschlussImport.Standort)) {
                        continue;
                    }

                    assignedHouses.Add(house.Item1);
                    hausAnschlusses.Add(new Hausanschluss(Guid.NewGuid().ToString(),
                        house.Item1.Guid,
                        hausanschlussImport.ObjectID,
                        hausanschlussImport.Egid,
                        hausanschlussImport.Isn,
                        hausanschlussImport.Lon,
                        hausanschlussImport.Lat,
                        hausanschlussImport.Trafokreis,
                        house.Item2,
                        0,
                        hausanschlussImport.Adress,
                        hausanschlussImport.Standort));
                }
            }

            Debug("#######################");
            Debug("Finished Direct assignments:");
            Debug("Imported Hausanschluss Locations: " + importedHausanschluss.Count);
            Debug("Houses:" + houses.Count);
            Debug("Hausanschlüsse:" + hausAnschlusses.Count);
            Debug("assignedHouses:" + assignedHouses.Count);
            Debug("#######################");
        }

        private void DoProximityAssignments([NotNull] [ItemNotNull] IReadOnlyCollection<House> houses,
                                            [NotNull] [ItemNotNull] List<House> assignedHouses,
                                            [NotNull] [ItemNotNull] List<HausanschlussImport> allIsnLocations,
                                            [NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses)
        {
            var isnLocations = allIsnLocations.Where(x => !x.ObjectID.ToLower().Contains("leuchte")).ToList();
            foreach (var isnLocation in isnLocations) {
                if (isnLocation.ObjectID.ToLower().Contains("leuchte")) {
                    throw new FlaException("Leuchte gefunden");
                }
            }

            foreach (var assignedHouse in assignedHouses) {
                //check if every assigned house is actually assigned.
                var r = hausanschlusses.FirstOrDefault(x => x.HouseGuid == assignedHouse.Guid);
                if (r == null) {
                    throw new FlaException("Assigned house is not actually assigned.");
                }
            }

            //warning: multiple houses can share a hausanschluss
            houses = houses.Distinct().ToList();
            foreach (var house in houses) {
                if (assignedHouses.Contains(house)) {
                    continue;
                }

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
                    var pickedImported = locations[0].Item2;
                    //if (locations[0].Item1 > 200) {
                    //throw new Exception("More than 200 m");
                    //}

                    hausanschlusses.Add(new Hausanschluss(Guid.NewGuid().ToString(),
                        house.Guid,
                        pickedImported.ObjectID,
                        pickedImported.Egid,
                        pickedImported.Isn,
                        pickedImported.Lon,
                        pickedImported.Lat,
                        pickedImported.Trafokreis,
                        HouseMatchingType.Proximity,
                        locations[0].Item1,
                        locations[0].Item2.Adress,
                        pickedImported.Standort));
                }
            }

            var allAssignedHa = hausanschlusses.Select(y => y.ObjectID);
            var allUnassignedHa = allIsnLocations.Where(x => !allAssignedHa.Contains(x.ObjectID)).ToList();
            foreach (var import in allUnassignedHa) {
                var houseDistances = new List<Tuple<double, House>>();
                var hawgs = new WgsPoint(import.Lon, import.Lat);
                foreach (var house in houses) {
                    var houseCoords = house.WgsGwrCoords.ToList();
                    houseCoords.AddRange(house.LocalWgsPoints);
                    var distance = hawgs.GetMinimumDistance(houseCoords);
                    houseDistances.Add(new Tuple<double, House>(distance, house));
                }

                houseDistances.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                var pickedHouse = houseDistances[0].Item2;

                hausanschlusses.Add(new Hausanschluss(Guid.NewGuid().ToString(),
                    pickedHouse.Guid,
                    import.ObjectID,
                    import.Egid,
                    import.Isn,
                    import.Lon,
                    import.Lat,
                    import.Trafokreis,
                    HouseMatchingType.Leftover,
                    houseDistances[0].Item1,
                    import.Adress,
                    import.Standort));
            }

            Info("Number of unassigned hausanschlüsse:" + allUnassignedHa.Count);
        }

        private void ExportAsExcel([NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses, [NotNull] [ItemNotNull] List<House> houses)
        {
            {
                //houses direct
                RowCollection rc = new RowCollection("Houses", "Häuser");
                foreach (House house in houses) {
                    rc.Rows.Add(MakeHouseRow(house));
                    foreach (var ha in house.Hausanschluss) {
                        rc.Rows.Add(MakeHausanschlussRow(ha));
                    }
                }

                var fn = MakeAndRegisterFullFilename("HousesWithHausanschluss.xlsx", Constants.PresentSlice);
                XlsxDumper.WriteToXlsx(fn, rc);
            }
            {
                //houses by Trafokreis
                RowCollection rc = new RowCollection("Houses", "Häuser");
                var trafokreise = hausanschlusses.Select(x => x.Trafokreis).Distinct().ToList();
                foreach (var trafokreis in trafokreise) {
                    var rbTk = RowBuilder.Start("Trafokreis Name", trafokreis);
                    var selectedHouses = houses.Where(x => x.Hausanschluss.Any(y => y.Trafokreis == trafokreis)).ToList();
                    rbTk.Add("House Count", selectedHouses.Count);
                    rc.Rows.Add(rbTk.GetRow());
                    foreach (House house in selectedHouses) {
                        rc.Rows.Add(MakeHouseRow(house));
                        foreach (var ha in house.Hausanschluss) {
                            rc.Rows.Add(MakeHausanschlussRow(ha));
                        }
                    }
                }

                var fn = MakeAndRegisterFullFilename("HousesWithTrafokreisTree.xlsx", Constants.PresentSlice);
                XlsxDumper.WriteToXlsx(fn, rc);
            }
        }

        [NotNull]
        [ItemNotNull]
        private static List<HausanschlussImport> LoadMergedHausanschlussImports([NotNull] MyDb dbRaw)
        {
            var isnLocations = dbRaw.Fetch<HausanschlussImport>();
            var isnLocationsSup = dbRaw.Fetch<HausanschlussImportSupplement>();
            var mergedLocs = new List<HausanschlussImport>();

            foreach (var sup in isnLocationsSup) {
                var mainlocs = isnLocations.Where(x =>
                        Math.Abs(x.Lat - sup.HaLat) < 0.00000000000001 && Math.Abs(x.Lon - sup.HaLon) < 0.00000000000001 &&
                        x.ObjectID == sup.HaObjectid)
                    .ToList();
                if (mainlocs.Count > 1) {
                    throw new FlaException("found multiple candidates for " + sup.HaObjectid);
                }

                if (mainlocs.Count == 0) {
                    throw new FlaException("No object found to match for " + sup.HaObjectid);
                }

                HausanschlussImport hai1 = new HausanschlussImport(mainlocs[0], sup.TargetStandort);
                hai1.Adress = sup.TargetComplexName;
                hai1.Isn = sup.TargetIsn;
                mergedLocs.Add(hai1);
            }

            foreach (var location in isnLocations) {
                mergedLocs.Add(location);
            }

            return mergedLocs;
        }

        [NotNull]
        private static Row MakeHausanschlussRow([NotNull] Hausanschluss ha)
        {
            var rbh = RowBuilder.Start("HA Adress", ha.Adress);
            rbh.Add("Trafokreis", ha.Trafokreis);
            rbh.Add("MatchingType", ha.MatchingType.ToString());
            rbh.Add("Distance", ha.Distance);
            rbh.Add("ObjektID", ha.ObjectID);
            rbh.Add("Isn", ha.Isn);
            return rbh.GetRow();
        }

        [NotNull]
        private static Row MakeHouseRow([NotNull] House house)
        {
            var rb = RowBuilder.Start("HouseName", house.ComplexName);
            rb.Add("Egid", house.EGIDsAsJson);
            rb.Add("ISN", house.GebäudeIDsAsJson);
            rb.Add("Localnet WGS", house.LocalWgsPointsAsJson);
            rb.Add("GWR WGS ", house.WgsGwrCoordsAsJson);
            var rb2 = rb.GetRow();
            return rb2;
        }
    }
}