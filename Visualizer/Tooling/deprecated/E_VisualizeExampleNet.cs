/*using System.Collections.Generic;
using BurgdorfStatistics.DataModel;
using BurgdorfStatistics.DataModel.Creation;
using BurgdorfStatistics.Mapper;
using BurgdorfStatistics.Tooling;
using JetBrains.Annotations;

namespace BurgdorfStatistics._05_PresentVisualizer
{
    // ReSharper disable once InconsistentNaming
    public class E_VisualizeExampleNet : RunableWithBenchmark
    {
        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<House>(Stage.PresentVisualisation, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouses.Database.Fetch<House>();
            List<MapPoint> mapPoints = new List<MapPoint>();
            foreach (var house in houses) {
                RGB MakeColor(House h) => Constants.Black;
                mapPoints.Add(house.GetMapPoint(MakeColor));
            }
            //OsmMapDrawer omd = new OsmMapDrawer(Services.Logger);
              //  omd.MakeMap(mapPoints,@"d:\mymap.png");

          /*  var dbVisual = SqlConnection.GetDatabaseConnection(Stage.PresentVisualisation, Constants.PresentSlice);
            
            var households = dbHouses.Database.Fetch<PotentialHousehold>();
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var localnet = dbRaw.Fetch<Localnet>();
            var occupants = dbHouses.Database.Fetch<Occupant>();
            Log(MessageType.Info,"Loaded house data: " + houses.Count);
            List<MapPoint> housemap = new List<MapPoint>();
            List<MapPoint> anzahlWohnungen = new List<MapPoint>();
            int missingEntries = 0;
            var ids = GetIds();
            dbVisual.Database.BeginTransaction();

            List<ExportEntry> exportEntries = new List<ExportEntry>();
            foreach (var id in ids) {
                House house = houses.FirstOrDefault(x => x.GebäudeObjectIDs.Contains(id));
                var localnetEntries = localnet.Where(x => x.ObjektIDGebäude == id).ToList();
                if(house?.GeoCoords.Count > 0)
                {
                    var householdsForThis = households.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    foreach (GeoCoord coord in house.GeoCoords)
                    {
                        housemap.Add(new MapPoint(coord.X, coord.Y, 10, 10));
                        anzahlWohnungen.Add(new MapPoint(coord.X, coord.Y, house.NumberOfHouseholds, (int)(Math.Max(Math.Log(house.NumberOfHouseholds), 1) * 10)));
                    }

                    house.HouseID = 0;
                    const string delimiter = ",";
                    var verrechnungstyparten = localnetEntries.Select(x => x.VerrechnungstypArt).Distinct().Aggregate((i, j) => i + delimiter + j);
                    var verrechnungstypKategorie = localnetEntries.Select(x => x.VerrechnungstypKategorie).Distinct().Aggregate((i, j) => i + delimiter + j);
                    house.Comments = "LocalnetEntries: " + localnetEntries.Count + "\n"
                                     + verrechnungstyparten + "\n"
                                     + verrechnungstypKategorie + "\n"
                                     + JsonConvert.SerializeObject(localnetEntries, Formatting.Indented);
                    dbVisual.Database.Save(house);
                    string gebäudeObjektIDs = "";
                    var builder = new System.Text.StringBuilder();
                    builder.Append(gebäudeObjektIDs);
                    foreach (int houseGebäudeObjectID in house.GebäudeObjectIDs)
                    {
                        builder.Append(houseGebäudeObjectID + ",");
                    }
                    gebäudeObjektIDs = builder.ToString();

                    foreach (PotentialHousehold hh in householdsForThis)
                    {
                        var people = occupants.Count(x => x.HouseholdGuid == hh.HouseholdGuid);
                        ExportEntry ee = new ExportEntry(
                            house.HouseGuid,
                            people,
                            hh.YearlyElectricityUse,
                            gebäudeObjektIDs);
                        exportEntries.Add(ee);
                    }
                }
                else {
                    Log(MessageType.Info, "Missing building ID: " +  id);
                    missingEntries++;
                }
            }
            dbVisual.Database.CompleteTransaction();
            Log(MessageType.Info, "Number of missing example net entries: " + missingEntries+ "/" + ids.Count);
            MapDrawer md = new MapDrawer(Services.Logger);
            const string section = "Test-Beispielnetz";
            const string sectionDescription = "";
            string dstFileName = dbVisual.GetFullFilename(@"ExampleMap_MapHouses.svg",SequenceNumber,Name);
            ResultFileEntry rfe = new ResultFileEntry(section,sectionDescription,
                dstFileName.Replace(".svg",".png"),"","",Scenario.Present,2017,Stage.PresentVisualisation);
            rfe.Save();
            md.DrawMapSvg(housemap, dstFileName , new List<MapLegendEntry>());
            dstFileName = dbVisual.GetFullFilename(@"ExampleMap_MapAnzahlWohnungen.svg",SequenceNumber,Name);
            md.DrawMapSvg(anzahlWohnungen, dstFileName, new    List<MapLegendEntry>());
            ResultFileEntry rfe2 = new ResultFileEntry(section, sectionDescription,
                dstFileName.Replace(".svg", ".png"), "", "", Constants.PresentSlice, Stage.PresentVisualisation);
            rfe2.Save();
            Log(MessageType.Info, "NumberOfMergedEntries written");
            using (StreamWriter sw = new StreamWriter(@"V:\BurgdorfStatistics\firstExport.json"))
            {
                sw.WriteLine(JsonConvert.SerializeObject(exportEntries, Formatting.Indented));
                sw.Close();
            }
        

        /*
        [NotNull]
        private static List<int> GetIds()
        {
            List<int> l = new List<int>
            {
                400210,
                400212,
                400214,
                400217,
                400219,
                400222,
                400223,
                400224,
                400226,
                400227,
                400228,
                400229,
                400230,
                400231,
                400232,
                400233,
                400253,
                400254,
                400255,
                400256,
                400257,
                400258,
                400259,
                400260,
                400261,
                400263,
                400268,
                405616,
                406479,
                408728,
                409231
            };
            return l;
        }
        
        public E_VisualizeExampleNet([NotNull] ServiceRepository services)
            : base("E_ExampleNet", Stage.PresentVisualisation,500,
                services,false)
        {
        }
    }
}
*/

