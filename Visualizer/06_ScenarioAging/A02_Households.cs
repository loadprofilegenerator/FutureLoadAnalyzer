using System.Collections.Generic;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {

    // ReSharper disable once InconsistentNaming
    public class A02_Households : RunableForSingleSliceWithBenchmark {
        public A02_Households([NotNull] ServiceRepository services)
            : base(nameof(A02_Households), Stage.ScenarioCreation, 101,
                services, false, new HouseholdCharts())
        {
            DevelopmentStatus.Add("add additional households to the existing houses");
            DevelopmentStatus.Add("Remove emigrating households");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            Log(MessageType.Debug, "running house copying");
            Services.SqlConnection.RecreateTable<Household>(Stage.Houses, slice);
            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, slice.PreviousScenarioNotNull).Database;
            var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, slice).Database;
            var dstHouses = dbDstHouses.Fetch<House>();
            var srcHouseholds = dbSrcHouses.Fetch<Household>();
            dbDstHouses.BeginTransaction();
            var houseHoldsByHouseKey = new Dictionary<string, List<Household>>();
            foreach (var hh in srcHouseholds) {
                if (!houseHoldsByHouseKey.ContainsKey(hh.HouseGuid)) {
                    houseHoldsByHouseKey.Add(hh.HouseGuid, new List<Household>());
                }
                houseHoldsByHouseKey[hh.HouseGuid].Add(hh);
            }
            /*
            WeightedRandomAllocator<House> wra = new WeightedRandomAllocator<House>(Services.Rnd);
            List<House> tornDownHouses = wra.PickObjects(srcHouses, (x => x.AverageBuildingAge), parameters.TornDownHouses);

            
            List<House> newHouses = new List<House>();
            for (int i = 0; i < parameters.NewlyBuiltHouses; i++) {
                //replace an existing house
                if (tornDownHouses.Count > 0) {
                    House pickedHouse = null;
                    pickedHouse = tornDownHouses[Services.Rnd.Next(tornDownHouses.Count)];
                    tornDownHouses.Remove(pickedHouse);
                }
                else
                {
                    var h = new House();
                    h.Area = pickedHouse 200 + Services.Rnd.NextDouble(1000);
                    h.AverageBuildingAge = 0;
                }
            }*/
            int householdsSaved = 0;
            foreach (var house in dstHouses) {
                if (houseHoldsByHouseKey.ContainsKey(house.HouseGuid)) {
                    var hhs = houseHoldsByHouseKey[house.HouseGuid];
                    foreach (var hh in hhs) {
                        hh.HouseholdID = 0;
                        dbDstHouses.Save(hh);
                        householdsSaved++;
                    }
                }
            }
            Log(MessageType.Info,"finished writing " + householdsSaved + " households");
            dbDstHouses.CompleteTransaction();
            Log(MessageType.Debug, "finished house copying");
        }
    }
}