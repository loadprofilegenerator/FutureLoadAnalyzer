using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using OfficeOpenXml;
using Visualizer.Visualisation.SingleSlice;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class ManualComplexCoordinates {
        public ManualComplexCoordinates([NotNull] string name, double lon, double lat)
        {
            Name = name;
            Lon = lon;
            Lat = lat;
        }

        [NotNull]
        public string Name { get; }
        public double Lon { get; }
        public double Lat { get; }

    }
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global

    public class A01_Housemaker : RunableWithBenchmark {
        public A01_Housemaker([NotNull] ServiceRepository services)
            : base(nameof(A01_Housemaker), Stage.Houses, 1, services, true,
                new HouseCharts(services,Stage.Houses))
        {
        }
        [NotNull]
        [ItemNotNull]
        public  List<ManualComplexCoordinates> ReadManualCoordinatesList()
        {

            string path = Path.Combine(Services.RunningConfig.Directories.BaseUserSettingsDirectory, "Corrections","ManualCoordinates.xlsx");
            ExcelPackage ep = new ExcelPackage(new FileInfo(path));
            int row = 2;
            ExcelWorksheet ws = ep.Workbook.Worksheets[1];
            List<ManualComplexCoordinates> ctm = new List<ManualComplexCoordinates>();
            while (ws.Cells[row, 1].Value != null)
            {
                string cname = (string)ws.Cells[row, 1].Value;
                double lat = (double)ws.Cells[row, 2].Value;
                double lon = (double)ws.Cells[row, 3].Value;
                ctm.Add(new ManualComplexCoordinates(cname.Trim(), lon,lat));
                row++;
            }
            ep.Dispose();
            return ctm;
        }
        protected override void RunActualProcess()
        {
            var dbComplex = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            //var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var buildingcomplexes = dbComplex.Fetch<BuildingComplex>();

            var dbComplexEnergy = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            var complexdata = dbComplexEnergy.Fetch<ComplexBuildingData>();
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouse.RecreateTable<House>();
            dbHouse.RecreateTable<PotentialHousehold>();
            //var sonnendach = dbRaw.Fetch<SonnenDach>();
            dbHouse.BeginTransaction();

            List<string> complexesToIgnore = new List<string> {
                "EGID191357110", "EGID1306724", "EGID1305755", "Fischermätteliweg nn"
            };
            //complexesToIgnore.Add("Bernstrasse 113a");
            //complexesToIgnore.Add("Finkhubelweg 8");
            //complexesToIgnore.Add("Friedhof 4");
            var manualCoordinates = ReadManualCoordinatesList();
            foreach (var complex in buildingcomplexes) {
                if (complexesToIgnore.Contains(complex.ComplexName)) {
                    continue;
                }
                //haus anlegen
                var h = new House(complex.ComplexName,
                    complex.ComplexGuid,
                    Guid.NewGuid().ToString()
                    ) {
                    ComplexID = complex.ComplexID,
                };
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // ReSharper disable once HeuristicUnreachableCode
                if (h.Guid == null) {
                    // ReSharper disable once HeuristicUnreachableCode
                    throw new Exception("House guid was null");
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                // ReSharper disable once HeuristicUnreachableCode
                if (h.ComplexGuid == null) {
                    // ReSharper disable once HeuristicUnreachableCode
                    throw new Exception("Complex guid was null");
                }

                //gebäudeobjektids zuordnen

                foreach (var complexGebäudeObjectID in complex.GebäudeObjectIDs) {
                    h.GebäudeObjectIDs.Add(complexGebäudeObjectID);
                }

                // egids zuordnen
                foreach (var eGid in complex.EGids) {
                    h.EGIDs.Add((int)eGid);
                }

                //geo coords zuordnen
                foreach (var coord in complex.Coords) {
                    h.WgsGwrCoords.Add(WgsPoint.ConvertKoordsToLonLat(coord.X, coord.Y));
                }

                var manualCoord = manualCoordinates.Where(x => x.Name == h.ComplexName).ToList();
                if (manualCoord.Any()) {
                    foreach (var manualComplexCoordinatese in manualCoord) {
                        h.WgsGwrCoords.Add(new WgsPoint(manualComplexCoordinatese.Lon, manualComplexCoordinatese.Lat));
                    }
                }

                foreach (var coord in complex.LocalnetCoords) {
                    h.LocalWgsPoints.Add(WgsPoint.ConvertKoordsToLonLat(coord.X, coord.Y));
                }

                //addressen zuordnen
                if (complex.Adresses.Count == 0) {
                    h.Adress = "Unknown";
                }
                else {
                    h.Adress = complex.Adresses[0];
                }

                if (complex.TrafoKreise.Count > 0) {
                    foreach (var erzId in complex.ErzeugerIDs) {
                        h.ErzeugerIDs.Add(erzId);
                    }
                }


                //assign household data
                var thiscomplexdata = complexdata.FirstOrDefault(x => x.ComplexName == complex.ComplexName);
                if (thiscomplexdata == null) {
                    h.Area = 0;
                    h.Appartments.Add(new AppartmentEntry(Guid.NewGuid().ToString(), 0, false, Constants.PresentSlice.DstYear));
                }
                else {
                    if (thiscomplexdata.AnzahlWohnungenBern > 0) {
                        double avgArea = thiscomplexdata.TotalEnergieBezugsfläche / thiscomplexdata.AnzahlWohnungenBern;
                        for (int i = 0; i < thiscomplexdata.AnzahlWohnungenBern; i++) {
                                h.Appartments.Add(new AppartmentEntry(Guid.NewGuid().ToString(), avgArea, true, Constants.PresentSlice.DstYear));
                        }
                    }
                    else {
                        h.Appartments.Add(new AppartmentEntry(Guid.NewGuid().ToString(), thiscomplexdata.TotalEnergieBezugsfläche, false,Constants.PresentSlice.DstYear));
                    }
                    h.Area += thiscomplexdata.TotalArea;
                    h.AverageBuildingAge = thiscomplexdata.BuildingAges.Average();
                }

                dbHouse.Save(h);
            }

            dbHouse.CompleteTransaction();
            var allHouses = dbHouse.FetchAsRepo<House>();
            RowCollection rc = new RowCollection("houses","houses");
            foreach (var house in allHouses) {
                RowBuilder rb = RowBuilder.Start("age",house.AverageBuildingAge);
                rc.Add(rb);
            }

            var fn = MakeAndRegisterFullFilename("HouseAges.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn,rc);
        }
    }
}