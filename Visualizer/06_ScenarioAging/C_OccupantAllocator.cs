using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class C_OccupantAllocator : RunableForSingleSliceWithBenchmark {
        public C_OccupantAllocator([NotNull] ServiceRepository services)
            : base(nameof(C_OccupantAllocator), Stage.ScenarioCreation, 300, services, false, new OccupantCharts())
        {
            DevelopmentStatus.Add("//todo: have people die");
            DevelopmentStatus.Add("//todo: Make Children");
            DevelopmentStatus.Add("//todo: Add Immigration");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Log(MessageType.Debug, "Allocating occupants");
                Services.SqlConnection.RecreateTable<Occupant>(Stage.Houses, parameters);
                var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenarioNotNull).Database;
                var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
                var srcOccupants = dbSrcHouses.Fetch<Occupant>();
                var yearsToAge = parameters.DstYear - parameters.PreviousScenario?.DstYear??throw new FlaException("No Previous Scenario Set");
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
                Log(MessageType.Info,"transfered " + occupantsWritten + " people");
                //make new children
                //
                //add immigration
                dbDstHouses.CompleteTransaction();
        }
        /*
        protected override void RunChartMaking(ScenarioSliceParameters slice)
        {
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, slice);
            var srcOccupants = dbHouses.Database.Fetch<Occupant>();
              //make the maps
            Log(MessageType.Info, "Making maps");
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

            Log(MessageType.Info, "Maps written");
        }*/
    }
}