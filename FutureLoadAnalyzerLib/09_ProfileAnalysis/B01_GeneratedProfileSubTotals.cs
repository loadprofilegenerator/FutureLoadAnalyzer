using System.Collections.Generic;
using System.Diagnostics;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    // ReSharper disable once InconsistentNaming
    public class B01_GeneratedProfileSubTotals : RunableForSingleSliceWithBenchmark {
        public B01_GeneratedProfileSubTotals([NotNull] ServiceRepository services) : base(nameof(B01_GeneratedProfileSubTotals),
            Stage.ProfileAnalysis, 201, services, false)
        {
            DevelopmentStatus.Add("Implement the other exports too");
        }

        [NotNull] private readonly Dictionary<AnalysisKey, ArchiveEntry> _entriesByKey = new Dictionary<AnalysisKey, ArchiveEntry>();


        [NotNull]
        private static List<AnalysisKey> GenerateKeysFromProsumer([NotNull] Prosumer prosumer)
        {
            List<AnalysisKey> keys = new List<AnalysisKey>();
            if (prosumer.TrafoKreis == null) {
                throw new FlaException("Trafokreis was null");
            }
            if (prosumer.ProviderName == null) {
                throw new FlaException("ProviderName was null");
            }

            if (prosumer.ProfileSourceName == "PV Profile" && prosumer.GenerationOrLoad != GenerationOrLoad.Generation) {
                throw  new FlaException("A pv provider is not saved as generation");
            }

            keys.Add(new AnalysisKey(prosumer.TrafoKreis,null, SumType.ByTrafokreis, prosumer.GenerationOrLoad,null,null,null));
            keys.Add(new AnalysisKey(prosumer.TrafoKreis, prosumer.ProviderName, SumType.ByTrafokreisAndProvider, prosumer.GenerationOrLoad, null,null,null));
            keys.Add(new AnalysisKey(null, prosumer.ProviderName, SumType.ByProvider, prosumer.GenerationOrLoad, null,null,null));
            keys.Add(new AnalysisKey(null, null, SumType.ByProfileSource, prosumer.GenerationOrLoad, null, prosumer.ProfileSourceName,null));
            keys.Add(new AnalysisKey(null, null, SumType.ByHouseholdComponentType, prosumer.GenerationOrLoad,null,null,prosumer.HouseComponentType.ToString()));
            return keys;
        }

        private void ProcessProsumer([NotNull] List<AnalysisKey> keys, [NotNull] Prosumer prosumer)
        {
            foreach (var key in keys) {
                var profile = prosumer.Profile;
                if (profile == null) {
                    throw new FlaException("profile was null");
                }
                if (!_entriesByKey.ContainsKey(key)) {
                    Trace("adding new key " + key.ToString());
                    ArchiveEntry ae = new ArchiveEntry(key.ToString(),key,profile,prosumer.GenerationOrLoad, prosumer.TrafoKreis ?? throw new FlaException("No trafokreis"));
                    _entriesByKey.Add(key,ae);
                    continue;
                }
                _entriesByKey[key].Profile = _entriesByKey[key].Profile.Add(profile, profile.Name);
            }
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbProfileExport = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.HouseProfiles);
            var sa = SaveableEntry<Prosumer>.GetSaveableEntry(dbProfileExport, SaveableEntryTableType.HouseLoad, Services.Logger);
            int count = 0;
            Stopwatch sw = Stopwatch.StartNew();
            foreach (var prosumer in sa.ReadEntireTableDBAsEnumerable()) {
                var keys = GenerateKeysFromProsumer(prosumer);
                ProcessProsumer(keys,prosumer);
                count++;
                if (count % 1000 ==0) {
                    Info("Subtotal processing load count: " + count   + " in a total of " + sw.Elapsed);
                }
            }

            count = 0;
            sw = Stopwatch.StartNew();
            var saGen = SaveableEntry<Prosumer>.GetSaveableEntry(dbProfileExport, SaveableEntryTableType.HouseGeneration, Services.Logger);
            foreach (var prosumer in saGen.ReadEntireTableDBAsEnumerable()) {
                var keys = GenerateKeysFromProsumer(prosumer);
                ProcessProsumer(keys, prosumer);
                count++;
                if (count % 1000 == 0) {
                    Info("Subtotal processing generation count: " + count + " in a total of " + sw.Elapsed);
                }
            }
            Debug("summed up count: " + _entriesByKey.Count);
            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saArchive = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
            saArchive.MakeCleanTableForListOfFields(false);
            foreach (var value in _entriesByKey.Values) {
                saArchive.AddRow(value);
            }
            saArchive.SaveDictionaryToDatabase(MyLogger);
            Debug("Finished saving prosumer loads to the db");

            if (Services.RunningConfig.MakeHouseSums) {
                //make house sums
                var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
                var houses = dbHouses.Fetch<House>();
                Dictionary<string, string> houseNames = new Dictionary<string, string>();
                foreach (var house in houses) {
                    houseNames.Add(house.Guid, house.ComplexName);
                }

                MakeHouseSums(dbArchive, sa, houseNames);
            }
        }

        private void MakeHouseSums([NotNull] MyDb dbArchive, [NotNull] SaveableEntry<Prosumer> sa, [NotNull] Dictionary<string, string> houseNames)
        {
            Debug("started summing up the houses");
            ArchiveEntry currentAeLoad = new ArchiveEntry("",
                new AnalysisKey(null, null, SumType.ByHouse, GenerationOrLoad.Load, "",null,null),
                Profile.MakeConstantProfile(0, "", Profile.ProfileResolution.QuarterHour),
                GenerationOrLoad.Load,"");
            ArchiveEntry currentAeGen = new ArchiveEntry("",
                new AnalysisKey(null, null, SumType.ByHouse, GenerationOrLoad.Generation, "",null,null),
                Profile.MakeConstantProfile(0, "", Profile.ProfileResolution.QuarterHour),
                GenerationOrLoad.Generation,"");
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedHouseProfiles, Services.Logger);
            saHouses.MakeCleanTableForListOfFields(false);
            int count = 0;
            List<string> processedHouseNames = new List<string>();
            foreach (var prosumer in sa.ReadEntireTableDBAsEnumerable("HouseGuid")) {
                count++;
                string houseName = houseNames[prosumer.HouseGuid];
                if (houseName != currentAeLoad.Key.HouseName && !string.IsNullOrWhiteSpace(currentAeLoad.Key.HouseName)) {
                    saHouses.AddRow(currentAeLoad);
                    saHouses.AddRow(currentAeGen);
                    if (saHouses.RowEntries.Count > 50) {
                        saHouses.SaveDictionaryToDatabase(MyLogger);
                    }
                    if (processedHouseNames.Contains(houseName)) {
                        throw new FlaException("Duplicate house name");
                    }
                    processedHouseNames.Add(houseName);
                }

                if (houseName != currentAeLoad.Key.HouseName) {
                    currentAeLoad = new ArchiveEntry(houseName,
                        new AnalysisKey(null, null, SumType.ByHouse, GenerationOrLoad.Load, houseName, null,null),
                        Profile.MakeConstantProfile(0, houseName, Profile.ProfileResolution.QuarterHour),
                        GenerationOrLoad.Load, prosumer.TrafoKreis ?? throw new FlaException("No trafokreis"));
                    currentAeGen = new ArchiveEntry(houseName,
                        new AnalysisKey(null, null, SumType.ByHouse, GenerationOrLoad.Generation, houseName,null,null),
                        Profile.MakeConstantProfile(0, houseName, Profile.ProfileResolution.QuarterHour),
                        GenerationOrLoad.Generation, prosumer.TrafoKreis);
                }

                switch (prosumer.GenerationOrLoad) {
                    case GenerationOrLoad.Generation:
                        currentAeGen.Profile = currentAeGen.Profile.Add(prosumer.Profile ?? throw new FlaException(), currentAeGen.Name);
                        break;
                    case GenerationOrLoad.Load:
                        currentAeLoad.Profile = currentAeLoad.Profile.Add(prosumer.Profile ?? throw new FlaException(), currentAeGen.Name);
                        break;
                    default: throw new FlaException("Forgotten type");
                }
                if (count % 1000 == 0) {
                    Info("Subtotal house Processing count: " + count);
                }
            }

            saHouses.SaveDictionaryToDatabase(MyLogger);
            Debug("Finished saving the remaining houses");
        }
    }
}