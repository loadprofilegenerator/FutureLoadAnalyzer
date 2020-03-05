/*
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.DataModel;
using BurgdorfStatistics.DataModel.Creation;
using BurgdorfStatistics.Mapper;
using BurgdorfStatistics.Tooling;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace BurgdorfStatistics.Exporter {

    public class AdaptricityHouseID {
        public AdaptricityHouseID(ISNLocation isn, House house, HouseMatchingType matchingType, double distance)
        {
            Isn = isn;
            House = house;
            MatchingType = matchingType;
            Distance = distance;
        }

        public ISNLocation Isn { get; set; }
        public House House { get; set; }
        public HouseMatchingType MatchingType { get; set; }
        public double Distance { get; set; }
    }

    public class DirenMatcher : RunableWithBenchmark {
        public DirenMatcher([NotNull] ServiceRepository services)
            : base(nameof(DirenMatcher), Stage.ValidationExporting, 3, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            //string excelFileName = @"U:\SimZukunft\RawDataForMerging\DatenfürEnergiebilanzBurgdorf2018.xlsx";
            //var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw,Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw,Constants.PresentSlice).Database;
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            //var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;

            var houses = dbHouses.Fetch<House>();
            var isnLocations = dbRaw.Fetch<ISNLocation>();
            var energyUses = dbHouses.Fetch<HouseSummedLocalnetEnergyUse>();
            //var complexEnergy = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
            //new Random(1);
            var assignedHouses = new List<House>();
            var houseIDs = new List<AdaptricityHouseID>();

            DoDirectAssignments(isnLocations, houses, assignedHouses, houseIDs);


            DoProximityAssignments(houses, assignedHouses, isnLocations, houseIDs);


            using (var p = new ExcelPackage()) {
                //A workbook must have at least on cell, so lets add one...
                var ws = p.Workbook.Worksheets.Add("MySheet");
                //To set values in the spreadsheet use the Cells indexer.
                //header
                ws.Cells[1, 1].Value = "ObjectID";
                ws.Cells[1, 2].Value = "EGID";
                ws.Cells[1, 3].Value = "ISN";
                ws.Cells[1, 4].Value = "Lon";
                ws.Cells[1, 5].Value = "Lat";
                ws.Cells[1, 6].Value = "HouseIds";
                ws.Cells[1, 7].Value = "Gesamter Stromverbrauch";

                var row = 2;
                foreach (var isn in isnLocations) {
                    ws.Cells[row, 1].Value = isn.ObjectID;
                    ws.Cells[row, 2].Value = isn.Egid;
                    ws.Cells[row, 3].Value = isn.Isn;
                    ws.Cells[row, 4].Value = isn.Lon;
                    ws.Cells[row, 5].Value = isn.Lat;
                    var matchedHouses = houseIDs.Where(x => x.Isn == isn).ToList();

                    var complexes = "";
                    double electricitySum = 0;
                    double gasSum = 0;
                    foreach (var adaptricityHouseID in matchedHouses) {
                        complexes += adaptricityHouseID.House.ComplexName + " (" + adaptricityHouseID.MatchingType.ToString() + ", " + adaptricityHouseID.Distance.ToString("F0") + "m) , ";
                        var e = adaptricityHouseID.House.CollectTotalEnergyConsumptionFromLocalnet(energyUses);
                        electricitySum += e.ElectricityUse;
                        gasSum += e.GasUse;
                    }

                    ws.Cells[row, 6].Value = complexes;
                    ws.Cells[row, 7].Value = electricitySum;
                    ws.Cells[row, 8].Value = gasSum;
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
                    var houseSum = house.CollectTotalEnergyConsumptionFromLocalnet(energyUses);
                    ws2.Cells[row, 1].Value = house.ComplexName;
                    ws2.Cells[row, 2].Value = house.EGIDsAsJson;
                    ws2.Cells[row, 3].Value = house.GebäudeIDsAsJson;
                    ws2.Cells[row, 4].Value = houseSum.ElectricityUse;
                    ws2.Cells[row, 5].Value = houseSum.GasUse;
                    var matchedIsn = houseIDs.Where(x => x.House == house).ToList();
                    var ids = string.Join(",", matchedIsn.Select(x => x.Isn.ObjectID + " (" + x.MatchingType.ToString() + ", " + x.Distance.ToString("F0") + ")"));
                    ws2.Cells[row, 6].Value = ids;
                    row++;
                }

                var xfilename = MakeAndRegisterFullFilename("Export.xlsx", Name, "",Constants.PresentSlice);
                //Save the new workbook. We haven't specified the filename so use the Save as method.
                p.SaveAs(new FileInfo(xfilename));
            }

            RGB HouseFunc(House h)
            {
                if (assignedHouses.Contains(h)) {
                    return Constants.Blue;
                }

                if (houseIDs.Any(x => x.House == h)) {
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
            foreach (AdaptricityHouseID adaptricityHouseID in houseIDs) {
                if(adaptricityHouseID.MatchingType == HouseMatchingType.Direct) {
                    continue;
                }

                foreach (WgsPoint coord in adaptricityHouseID.House.WgsGwrCoords) {

                    WgsPoint isn = new WgsPoint(adaptricityHouseID.Isn.Lon, adaptricityHouseID.Isn.Lat);
                    lines.Add(new LineEntry(coord,isn,adaptricityHouseID.House.ComplexName  + " - " + adaptricityHouseID.Isn.ObjectID));
                }

            }
            Services.PlotMaker.MakeOsmMap(Name, filename, houseEntries, new List<WgsPoint>(), labels, lines);
        }

        
    }
}*/