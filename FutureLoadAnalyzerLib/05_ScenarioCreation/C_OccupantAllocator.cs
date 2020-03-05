using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class C_OccupantAllocator : RunableForSingleSliceWithBenchmark {
        public C_OccupantAllocator([NotNull] ServiceRepository services)
            : base(nameof(C_OccupantAllocator), Stage.ScenarioCreation, 300, services,
                false, new OccupantCharts(services,Stage.ScenarioCreation))
        {
            DevelopmentStatus.Add("//todo: have people die");
            DevelopmentStatus.Add("//todo: Make Children");
            DevelopmentStatus.Add("//todo: Add Immigration");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
         /*   Info( "Allocating occupants");
            Services.SqlConnectionPreparer.RecreateTable<Occupant>(Stage.Houses, slice);
                var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
                var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
                var srcOccupants = dbSrcHouses.Fetch<Occupant>();
                var yearsToAge = slice.DstYear - slice.PreviousSlice?.DstYear??throw new FlaException("No Previous Scenario Set");
                dbDstHouses.BeginTransaction();
                int occupantsWritten = 0;
                foreach (var occ in srcOccupants) {
                    occ.OccupantID = 0;
                    occ.Age += yearsToAge;
                    if (occ.Age < 100) {
                        dbDstHouses.Save(occ);
                        occupantsWritten++;
                    }
                }
                Info("transfered " + occupantsWritten + " people");
                //make new children
                //
                //add immigration
                dbDstHouses.CompleteTransaction();*/
        }
        /*
        protected override void RunChartMaking(ScenarioSliceParameters slice)
        {
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, slice);
            var srcOccupants = dbHouses.Database.Fetch<Occupant>();
              //make the maps
            Info( "Making maps");
            var houses = dbHouses.Database.Fetch<House>();
            var households = dbHouses.Database.Fetch<Household>();
            var agepoints = new List<MapPoint>();
            var peoplepoints = new List<MapPoint>();
            foreach (var house in houses)
            {
                if (house.WgsGwrCoords.Count == 0)
                {
                    continue;
                }

                var householdsInHouse = households.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                var people = new List<Occupant>();
                foreach (var household1 in householdsInHouse)
                {
                    var occupants = srcOccupants.Where(x => x.HouseholdGuid == household1.HouseholdGuid);
                    people.AddRange(occupants);
                }

                var geoco = house.WgsGwrCoords[0];
                double averageAge = 0;
                if (people.Count > 0)
                {
                    averageAge = people.Average(x => x.Age);
                }

                agepoints.Add(new MapPoint(geoco.Lat, geoco.Lon, averageAge, 10));
                peoplepoints.Add(new MapPoint(geoco.Lat, geoco.Lon, people.Count, people.Count));
            }

            var dstFileName2 = dbHouses.GetFullFilename(slice.GetFileName() + "_Age_Map.svg", SequenceNumber, Name);
            Services.MapDrawer.DrawMapSvg(agepoints, dstFileName2, new List<MapLegendEntry>());

            section = "Altersverteilungs-Karte";
            sectionDescription = "Altersverteilungen";
            var rfe2 = new ResultFileEntry(section, sectionDescription,
                dstFileName2.Replace(".svg", ".png"), slice.GetFileName(), "",
                slice, Stage.ScenarioVisualisation);
            rfe2.Save();

            section = "Personenverteilungs-Karte";
            sectionDescription = "Personenverteilungen";
            var dstFileName3 = dbHouses.GetFullFilename(slice.GetFileName() + "_PersonCount_Map.svg", SequenceNumber, Name);
            Services.MapDrawer.DrawMapSvg(peoplepoints, dstFileName3, new List<MapLegendEntry>());
            var rfe3 = new ResultFileEntry(section, sectionDescription,
                dstFileName3.Replace(".svg", ".png"), slice.GetFileName(),
                "", slice, Stage.ScenarioVisualisation);
            rfe3.Save();

            Info( "Maps written");
        }*/
    }
}