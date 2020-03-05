using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Tooling.Database;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BurgdorfStatistics._10_ProfileExport {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class Z01_ProfileMerger : RunableForSingleSliceWithBenchmark {
        public Z01_ProfileMerger([NotNull] ServiceRepository services) : base(nameof(Z01_ProfileMerger),
            Stage.ProfileExport, 100, services, false)
        {
            DevelopmentStatus.Add("Implement the other exports too");
            DevelopmentStatus.Add("enable the total validation and fix any errors. seems at least one house is not getting exported.");
        }

        private readonly bool _validateAllAssigned = false;
        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<HausAnschlussExportEntry>(Stage.ProfileExport,parameters);
            MyDb dbProfileExport = PrepareProfileExport(parameters);

            //prepare profile export

            //load previous data
            var dbProfileGeneration = LoadPreviousData(parameters,
                out var houses, out var households,
                out var businesses);
            //trafokreise
            var trafokreise =houses.SelectMany(x=> x.Hausanschluss).Select(x => x.Trafokreis).Distinct().ToList();
            //int trafokreiscount = 1;

            dbProfileGeneration.Database.BeginTransaction();
            RowCollection rc = new RowCollection();
            foreach (var trafokreis in trafokreise) {

                //prepare
                if (string.IsNullOrWhiteSpace(trafokreis)) {
                    continue;
                }
              //  if (trafokreiscount > 3) {
                //    continue;
                //}
              //  trafokreiscount++;

                //merge
                var houseExportEntries = MergeAllEntriesForTrafokreis(houses, dbProfileGeneration, trafokreis,
                    households,businesses);

                //validate
                CheckHouseExportEntryIfAllHouseholdsAreCovered(households, houseExportEntries);

                //save
                var saLoad = Prosumer.GetSaveableEntry(dbProfileExport, TableType.HouseLoad);
                var saGeneration = Prosumer.GetSaveableEntry(dbProfileExport, TableType.HouseGeneration);
                foreach (var houseExportEntry in houseExportEntries) {
                    var rb = RowBuilder.Start("Trafokreis", trafokreis);
                    //load
                    var ps = new Prosumer(houseExportEntry.HouseGuid,
                        houseExportEntry.HouseName, ProsumerType.HouseLoad,
                        houseExportEntry.HouseGuid, houseExportEntry.Isn, houseExportEntry.Trafokreis,
                        houseExportEntry.GetSumProfile(HausAnschlussExportEntry.TypeOfSum.Load),houseExportEntry.HausanschlussGuid, houseExportEntry.HausAnschlussKeyString);
                    double profileEnergy = ps.Profile?.EnergySum() ?? throw new Exception("Invalid profile");
                    rb.Add("Housename", houseExportEntry.HouseName)
                        .Add("Load in Profile", profileEnergy).Add("Total Energy Load Planned", houseExportEntry.TotalEnergyLoad);
                    if (Math.Abs(profileEnergy - houseExportEntry.TotalEnergyLoad) > 1) {
                        throw new Exception("Wrong energy");
                    }
                    saLoad.AddRow(ps);
                    //generation
                    if (houseExportEntry.TotalEnergyGeneration > 1) {
                        var psGen = new Prosumer(houseExportEntry.HouseGuid, houseExportEntry.HouseName, ProsumerType.HouseLoad, houseExportEntry.HouseGuid, houseExportEntry.Isn,
                            houseExportEntry.Trafokreis, houseExportEntry.GetSumProfile(HausAnschlussExportEntry.TypeOfSum.Generation), houseExportEntry.HausanschlussGuid,
                            houseExportEntry.HausAnschlussKeyString);
                        double profileEnergyGen = psGen.Profile?.EnergySum() ?? throw new Exception("Invalid profile");
                        if (Math.Abs(profileEnergyGen - houseExportEntry.TotalEnergyGeneration) > 1) {
                            throw new Exception("Wrong energy");
                        }
                        rb.Add("Generation Profile Energy",profileEnergyGen)
                            .Add("Total Energy Generation Planned", houseExportEntry.TotalEnergyGeneration);
                        saGeneration.AddRow(psGen);
                    }

                    dbProfileExport.Database.Save(houseExportEntry);
                    rc.Add(rb);
                }
                saLoad.SaveDictionaryToDatabase();
                if (saGeneration.RowEntries.Count > 0) {
                    saGeneration.SaveDictionaryToDatabase();
                }
            }
            var fnLoad = MakeAndRegisterFullFilename("MergedStuff.xlsx", Name, "", parameters);
            XlsxDumper.WriteToXlsx(rc,fnLoad,"ProsumersMerged");
        }

        private void CheckHouseExportEntryIfAllHouseholdsAreCovered([NotNull] [ItemNotNull] List<Household> households,
                                                                    [NotNull] [ItemNotNull] List<HausAnschlussExportEntry> hees)
        {
            //Todo: same thing for businesses and other housecomponents
            var hhGuids = households.Select(x => x.HouseholdGuid).ToList();

            foreach (var exportEntry in hees) {
                var assignedGuids = exportEntry.LoadProsumers.Select(y => y.SourceGuid).ToList();
                var hhs = households.Where(x => x.HausAnschlussGuid == exportEntry.HausanschlussGuid).ToList();
                foreach (var hh in hhs) {
                    hhGuids.Remove(hh.HouseholdGuid);
                    if (!assignedGuids.Contains(hh.HouseholdGuid)) {
                        throw new Exception("Missing Household Guid");
                    }
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        private List<HausAnschlussExportEntry> MergeAllEntriesForTrafokreis([NotNull] [ItemNotNull] List<House> houses, [NotNull] MyDb dbProfileGeneration, [NotNull] string trafokreis,
                                                                           [NotNull] [ItemNotNull] List<Household> households,
                                                                            [ItemNotNull][NotNull] List<BusinessEntry> businesses)
        {
            var hausAnschlusses = houses.SelectMany(x => x.Hausanschluss).ToList();
            var hakeys =  hausAnschlusses.Where(x => x.Trafokreis == trafokreis).Select(x=>x.ObjectID).Distinct().ToList();
            var hees = new List<HausAnschlussExportEntry>();
            //List<double> zeroes = new List<double>(new double[8760 * 4]);
            //für jeden anschluss die prosumer sammeln
            foreach (var haKey in hakeys) {
                var hausanschlüsse = hausAnschlusses.Where(x => x.ObjectID == haKey).ToList();
                var hee = new HausAnschlussExportEntry(hausanschlüsse[0].HausanschlussGuid,
                    "", trafokreis,
                    hausanschlüsse[0].Isn, "", haKey);
                string hausname = "";
                foreach (var anschluss in hausanschlüsse) {
                    var house = houses.Single(x => x.HouseGuid == anschluss.HouseGuid);
                    hausname += house.ComplexName;
                    var assignedProfiles = Prosumer.LoadProsumers(dbProfileGeneration, TableType.HousePart,
                        anschluss.HouseGuid);
                    //households
                    var selectedHH = households.Where(x => x.HausAnschlussGuid == anschluss.HausanschlussGuid).ToList();
                    foreach (Household household in selectedHH) {
                        Prosumer hhp = assignedProfiles.Single(x => x.SourceGuid == household.HouseholdGuid);
                        //remove all profiles that are processed
                        assignedProfiles.Remove(hhp);
                        double usedEnergy = hhp.Profile?.EnergySum() ?? throw new Exception("Profile was null");
                        hee.LoadProsumers.Add(hhp);
                        hee.TotalEnergyLoad += usedEnergy;
                    }

                    //businesses
                    var selectedBusinesses = businesses.Where(x => x.HausAnschlussGuid == anschluss.HausanschlussGuid);
                    foreach (var business in selectedBusinesses) {
                        Prosumer hhp = assignedProfiles.Single(x => x.SourceGuid == business.BusinessGuid);
                        //remove all profiles that are processed
                        assignedProfiles.Remove(hhp);
                        double usedEnergy = hhp.Profile?.EnergySum() ?? throw new Exception("Profile was null");
                        hee.LoadProsumers.Add(hhp);
                        hee.TotalEnergyLoad += usedEnergy;
                    }

                    var pvprofiles = assignedProfiles.Where(x => x.ProsumerType == ProsumerType.PV).ToList();
                    foreach (Prosumer pvprofile in pvprofiles) {
                        //remove all profiles that are processed
                        assignedProfiles.Remove(pvprofile);
                        double generaetedEnergy = pvprofile.Profile?.EnergySum() ?? throw new Exception("Profile was null");
                        hee.GenerationProsumers.Add(pvprofile);
                        hee.TotalEnergyGeneration += generaetedEnergy;
                    }

                    if (_validateAllAssigned && assignedProfiles.Count > 0) {
                        var profiletypes = assignedProfiles.Select(x => x.ProsumerType).Distinct().ToList();
                        var profiletypesstr = JsonConvert.SerializeObject(profiletypes);
                        throw new FlaException("Leftover profiles that were not processed: " + assignedProfiles.Count  + ": " + profiletypesstr);
                    }
                    if(hee.TotalEnergyGeneration > 0 && hee.GenerationProsumers.Count == 0)
                    {
                        throw new FlaException("No generation set, but generating energy");
                    }
                }
                hee.HouseName = hausname;
                hees.Add(hee);
            }
            return hees;
        }

        [NotNull]
        private MyDb PrepareProfileExport([NotNull] ScenarioSliceParameters parameters)
        {
            SqlConnection.RecreateTable<HausAnschlussExportEntry>(Stage.ProfileExport, parameters);
            var dbProfileExport = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileExport, parameters);
            var fieldDefinition = Prosumer.GetSaveableEntry(dbProfileExport, TableType.HouseLoad);
            fieldDefinition.MakeTableForListOfFields();
            var resultPath = dbProfileExport.GetResultFullPath(SequenceNumber, Name);
            if (!Directory.Exists(resultPath)) {
                Directory.CreateDirectory(resultPath);
            }
            var saLoad1 = Prosumer.GetSaveableEntry(dbProfileExport, TableType.HouseLoad);
            saLoad1.MakeTableForListOfFields();
            var saGeneration1 = Prosumer.GetSaveableEntry(dbProfileExport, TableType.HouseGeneration);
            saGeneration1.MakeTableForListOfFields();
            return dbProfileExport;
        }

        [NotNull]
        private MyDb LoadPreviousData([NotNull] ScenarioSliceParameters parameters, [NotNull] [ItemNotNull] out List<House> houses, [NotNull][ItemNotNull] out List<Household> households,
                                      [ItemNotNull][NotNull] out List<BusinessEntry> businesses)
        {
            var dbProfileGeneration = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters);
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters);
            houses = dbHouses.Database.Fetch<House>();
            households = dbHouses.Database.Fetch<Household>();
            businesses = dbHouses.Database.Fetch<BusinessEntry>();
            return dbProfileGeneration;
        }
    }
}