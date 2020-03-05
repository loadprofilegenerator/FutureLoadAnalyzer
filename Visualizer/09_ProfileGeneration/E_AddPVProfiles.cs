using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics._08_ProfileImporter;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ProfileType = Data.DataModel.Profiles.ProfileType;

namespace BurgdorfStatistics._09_ProfileGeneration {
    /// <summary>
    /// make the pv profiles as prosumers
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class E_AddPVProfiles : RunableForSingleSliceWithBenchmark {
        public E_AddPVProfiles([NotNull] ServiceRepository services)
            : base(nameof(E_AddPVProfiles), Stage.ProfileGeneration, 500,
                services, true)
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            var dbHouse = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters);
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters);
            //var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            var dbPVProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGenerationPV, Constants.PresentSlice);
            int profileCount = Profile.CountProfiles(dbPVProfiles, TableType.PVGeneration);
            if (profileCount < 100) {
                throw new FlaException("No profiles in the database. Not generated for this slice perhaps?");
            }
            var pvsystems = dbHouse.Database.Fetch<PvSystemEntry>();
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var houses = dbHouses.Fetch<House>();
            Prosumer.ClearProsumerTypeFromDB(Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration,
                parameters),  ProsumerType.PV, TableType.HousePart);
            if (pvsystems.Count == 0) {
                throw new FlaException("No pv systems found");
            }
            Log(MessageType.Info, "Making " + pvsystems.Count + " pvsystems");
            var sa = Prosumer.GetSaveableEntry(dbDstProfiles, TableType.HousePart);

            //count total number of unique systems
            List<string> allKeys = new List<string>();
            foreach (PvSystemEntry pvsystem in pvsystems) {
                foreach (PVSystemArea area in pvsystem.PVAreas) {
                    int myazimut = (int)area.Azimut + 180;
                    if (myazimut == 360) {
                        myazimut = 0;
                    }

                    G_PVProfileGeneration.PVSystemKey pvk = new G_PVProfileGeneration.PVSystemKey(myazimut, (int)area.Tilt);
                    allKeys.Add(pvk.GetKey());
                }
            }

            var keys = allKeys.Distinct().OrderBy(x => x);
            Info("Total unique keys: " + keys.Count());

            //dumping for debugging
            var fn = MakeAndRegisterFullFilename("MyKeys.csv", Name, "", parameters);
            File.WriteAllText(fn,JsonConvert.SerializeObject(keys, Formatting.Indented));


            var fn2 = MakeAndRegisterFullFilename("MyKeysPredefined.csv", Name, "", parameters);
            var presetKeys = Profile.LoadAllKeys(dbPVProfiles, TableType.PVGeneration);
            File.WriteAllText(fn2, JsonConvert.SerializeObject(presetKeys, Formatting.Indented));

            Info("Wrote keys to " + fn);
            double totalEnergyFromAllSystems = 0;
            double totalProfileEnergy = 0;

            for (var idx = 0; idx < pvsystems.Count; idx++)
            {
                var pvsystem = pvsystems[idx];
                if (pvsystem.PVAreas.Count == 0) {
                    throw new FlaException("No PV Areas were defined.");
                }
                House house = houses.Single(x => x.HouseGuid == pvsystem.HouseGuid);
                Hausanschluss ha = house.Hausanschluss.Single(x => x.HausanschlussGuid == pvsystem.HausAnschlussGuid);

                var pa = new Prosumer(pvsystem.HouseGuid, house.ComplexName,
                    ProsumerType.PV, pvsystem.PvGuid,
                    pvsystem.FinalIsn, pvsystem.HausAnschlussGuid, ha.ObjectID);
                Profile p=null;
                double totalEnergyForThisSystem = 0;
                foreach (PVSystemArea area in pvsystem.PVAreas) {
                    G_PVProfileGeneration.PVSystemKey pvk = new G_PVProfileGeneration.PVSystemKey((int)area.Azimut + 180, (int)area.Tilt);
                    totalEnergyForThisSystem += area.Energy;
                    var singleProfile = Profile.LoadProfile(dbPVProfiles, TableType.PVGeneration,pvk.GetKey());
                    if (singleProfile.Count == 0) {
                        throw new FlaException("No profile was found: " + pvk.GetKey());
                    }
                    if (singleProfile.Count > 1)
                    {
                        throw new FlaException("too many profiles found: " + pvk.GetKey());
                    }

                    singleProfile[0].ProfileType = ProfileType.Energy;
                    singleProfile[0] = singleProfile[0].ScaleToTargetSum(area.Energy, singleProfile[0].Name);
                    if (Math.Abs(singleProfile[0].EnergySum() - area.Energy) > 0.1) {
                        throw new FlaException("invalid energy");
                    }
                    if (p == null) {
                        p = singleProfile[0];
                    }
                    else {
                        p = p.Add(singleProfile[0], p.Name);
                    }
                }

                if (Math.Abs(totalEnergyForThisSystem) < 1) {
                    throw new FlaException("PV profile with 0 energy found.");
                }

                if (p == null) {
                    throw new FlaException("No profile for " + pvsystem.Name + ( " idx:" + idx));
                }
                pa.Profile = p;
                pa.SumElectricityPlanned = totalEnergyForThisSystem;
                totalEnergyFromAllSystems += totalEnergyForThisSystem;
                sa.RowEntries.Add(pa.GetRow());
                if (pa.Profile == null) {
                    throw new FlaException("No profile");
                }
                totalProfileEnergy += pa.Profile.EnergySum();
                if (Math.Abs(totalEnergyFromAllSystems - totalProfileEnergy) > 1)
                {
                    throw new FlaException("energy sums not equal between planned energy and energy in profiles");
                }
                if (sa.RowEntries.Count > 100)
                {
                    sa.SaveDictionaryToDatabase();
                }
            }
            sa.SaveDictionaryToDatabase();
            if (Math.Abs(totalEnergyFromAllSystems - totalProfileEnergy) > 1)
            {
                throw new FlaException("energy sums not equal between planned energy and energy in profiles");
            }
            Info("Total energy from all pv systems: " + totalEnergyFromAllSystems.ToString("N"));

        }
    }
}