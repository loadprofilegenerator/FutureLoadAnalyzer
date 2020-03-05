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
    /// make the business profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class D_AddBusinessProfiles : RunableForSingleSliceWithBenchmark {
        public D_AddBusinessProfiles([NotNull] ServiceRepository services)
            : base(nameof(D_AddBusinessProfiles), Stage.ProfileGeneration, 400, services, false)
        {
            DevelopmentStatus.Add("Make visualisations of the sum profile of each household");
            DevelopmentStatus.Add("add the original isn of each business to the business entry");
            DevelopmentStatus.Add("use the correct business isn");
            DevelopmentStatus.Add("add the total energy checks back in");
            DevelopmentStatus.Add("connect the business to the correct hausanschluss");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            var vdewvals = dbSrcProfiles.Fetch<VDEWProfileValues>();
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters);
            Prosumer.ClearProsumerTypeFromDB(Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration,
                parameters),  ProsumerType.Household, TableType.HousePart);
            var slp = new SLPProvider(parameters.DstYear);
            //var hausAnschlüsse = dbHouses.Fetch<Hausanschluss>();
            //var houses = dbHouses.Fetch<House>();
            var buisineses = dbHouses.Fetch<BusinessEntry>();
            //double totalHouseholdEnergy = 0;
            //double totalProfileEnergy = 0;
            var sa = Prosumer.GetSaveableEntry(dbDstProfiles, TableType.HousePart);
            Log(MessageType.Info, "making " + buisineses.Count + " businesses");
            foreach (var be in buisineses) {
               // Hausanschluss ha = hausAnschlüsse.Single(x => x.HausanschlussGuid == be.HausAnschlussGuid);
                var pa = new Prosumer(be.HouseGuid, be.StandortIDsAsJson, ProsumerType.BusinessNoLastgang,
                    be.BusinessGuid, be.FinalIsn, be.HausAnschlussGuid,"") {
                    Profile = slp.Run(vdewvals, "G0", be.LowVoltageYearlyTotalElectricityUse),
                    SumElectricityPlanned = be.LowVoltageYearlyTotalElectricityUse
                };
                //totalHouseholdEnergy += pa.SumElectricityPlanned;
                //totalProfileEnergy += (double)pa.Profile?.EnergySum();
                sa.AddRow(pa);
            }
            /*var trafoKreise = dbDstProfiles.Database.Fetch<TrafoKreisResult>();
            double plannedHouseholdEnergy = trafoKreise.Sum(x => x.BusinessEnergy);
            if (Math.Abs(plannedHouseholdEnergy - totalHouseholdEnergy) > 1)
            {
                throw new Exception("energy sums are not equal between planned energy and allocated household energy");
            }

            if (Math.Abs(plannedHouseholdEnergy - totalProfileEnergy) > 1)
            {
                throw new Exception("energy sums not equal between planned energy and energy in profiles");
            }*/

            sa.SaveDictionaryToDatabase();
        }
    }
}