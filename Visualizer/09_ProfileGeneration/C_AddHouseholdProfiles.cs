using System;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace BurgdorfStatistics._09_ProfileGeneration {
    /// <summary>
    /// make the household profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class C_AddHouseholdProfiles : RunableForSingleSliceWithBenchmark {
        public C_AddHouseholdProfiles([NotNull] ServiceRepository services)
            : base(nameof(C_AddHouseholdProfiles), Stage.ProfileGeneration, 300, services, false)
        {
            DevelopmentStatus.Add("Make visualisations of the household profiles for each trafostation");
            DevelopmentStatus.Add("Add the checks for the total energy again");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters);
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            var vdewvals = dbSrcProfiles.Fetch<VDEWProfileValues>();
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var houses = dbHouses.Fetch<House>();
            Prosumer.ClearProsumerTypeFromDB(Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration,
                parameters),  ProsumerType.Household, TableType.HousePart);
            var slp = new SLPProvider(parameters.DstYear);
            //var houses = dbHouses.Fetch<House>();
            var households = dbHouses.Fetch<Household>();
            //var trafoKreise = dbDstProfiles.Database.Fetch<TrafoKreisResult>();
            Log(MessageType.Info, "making " + households.Count + " households");
            var sa = Prosumer.GetSaveableEntry(dbDstProfiles, TableType.HousePart);
            // && idx < 20
            double totalHouseholdEnergy = 0;
            double totalProfileEnergy = 0;
            for (var idx = 0; idx < households.Count; idx++) {
                var household = households[idx];
                House house = houses.Single(x => x.HouseGuid == household.HouseGuid);
                Hausanschluss ha = house.Hausanschluss.Single(x => x.HausanschlussGuid == household.HausAnschlussGuid);
                // ReSharper disable once UseObjectOrCollectionInitializer
                var pa = new Prosumer(household.HouseGuid, household.StandortIDsAsJson,
                    ProsumerType.Household, household.HouseholdGuid,
                    household.FinalIsn, household.HausAnschlussGuid,ha.ObjectID);
                pa.Profile = slp.Run(vdewvals, "H0", household.LowVoltageYearlyTotalElectricityUse);
                pa.SumElectricityPlanned = household.LowVoltageYearlyTotalElectricityUse;
                sa.RowEntries.Add(pa.GetRow());
                totalHouseholdEnergy += pa.SumElectricityPlanned;
                totalProfileEnergy += pa.Profile?.EnergySum()??0;
                if (sa.RowEntries.Count > 1000) {
                    sa.SaveDictionaryToDatabase();
                }
            }
            sa.SaveDictionaryToDatabase();
            if (Math.Abs(totalHouseholdEnergy - totalProfileEnergy) > 1) {
                throw new FlaException("energy sums not equal between planned energy and energy in profiles");
            }
            Info("Total Household energy: " + totalHouseholdEnergy);
            /*
            var loadedProsumers = Prosumer.LoadProsumers(dbDstProfiles, TableType.HousePart);
        foreach (Prosumer loadedProsumer in loadedProsumers) {
            Log(MessageType.Info,loadedProsumer.Name + " - " + loadedProsumer.SumElectricityFromProfile + " - " + loadedProsumer.Profile?.Values.Sum() );
        }*/
            //dbDstProfiles.Database.CompleteTransaction();
        }
    }
}