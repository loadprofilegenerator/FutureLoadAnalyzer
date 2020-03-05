/*using System;
using System.Linq;
using BurgdorfStatistics.DataModel;
using BurgdorfStatistics.DataModel.Creation;
using BurgdorfStatistics.DataModel.Profiles;
using BurgdorfStatistics.Tooling;
using JetBrains.Annotations;

namespace BurgdorfStatistics._09_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class A02_MakeTrafoKreise : RunableForSingleSliceWithBenchmark {
        public A02_MakeTrafoKreise([NotNull] ServiceRepository services) : base(nameof(A02_MakeTrafoKreise), Stage.ProfileGeneration, 101, services, false)
        {
            DevelopmentStatus.Add(" //make trafostations");
            DevelopmentStatus.Add("//calculate total energy in each trafostation");
            DevelopmentStatus.Add("//get energy sum from:");
            DevelopmentStatus.Add("//households");
            DevelopmentStatus.Add("//business");
            DevelopmentStatus.Add("//streetlight");
            DevelopmentStatus.Add("//pv");
            DevelopmentStatus.Add("//make month-profile for each trafostation");
            DevelopmentStatus.Add("//get total profile");
            DevelopmentStatus.Add("//get rlms");
            DevelopmentStatus.Add("//calculate residual");
            DevelopmentStatus.Add("//collect profiles for individual");
            DevelopmentStatus.Add("//visualize sum profile");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<TrafoKreisResult>(Stage.ProfileGeneration, parameters);

            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            dbSrcHouses.Execute("VACUUM");
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters).Database;

            var households = dbSrcHouses.Fetch<Household>();
            var businessEntries = dbSrcHouses.Fetch<BusinessEntry>();
            var srcHouses = dbSrcHouses.Fetch<House>();
            if (srcHouses.Count == 0) {
                throw new Exception("No houses were found");
            }

            dbDstProfiles.BeginTransaction();
            var trafokreise = srcHouses.Select(x => x.TrafoKreis).Distinct().ToList();
            foreach (var tkname in trafokreise) {
                var tk = new TrafoKreisResult {
                    TrafoKreisName = tkname,
                    TrafoGuid = Guid.NewGuid().ToString()
                };
                var housesInTk = srcHouses.Where(x => x.TrafoKreis == tkname).ToList();
                var houseGuids = housesInTk.Select(x => x.HouseGuid).ToList();
                tk.HouseGuids.AddRange(houseGuids);
                foreach (var house in housesInTk) {
                    House.TotalHouseEnergy houseEnergy = house.CollectTotalElectricityConsumption(households, businessEntries);
                    tk.HouseholdEnergy += houseEnergy.Households;
                    tk.BusinessEnergy += houseEnergy.Businesses;
                    tk.OtherEnergy += houseEnergy.Other;
                    tk.TotalEnergyAmount += houseEnergy.Total;
                    tk.TkEnergyEntries.Add(new TkEnergyEntry(house.HouseGuid, house.ComplexName,
                        houseEnergy.Total , house.GebäudeIDsAsJson));
                }

                dbDstProfiles.Save(tk);
            }

            dbDstProfiles.CompleteTransaction();
        }
    }
}*/