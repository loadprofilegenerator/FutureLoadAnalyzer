using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class CachingLPGProfileLoader : BasicLoggable {
        [NotNull] private readonly DBDto _dbDto;
        [NotNull] private readonly Dictionary<string, HouseholdKeyEntryList> _householdKeyEntries = new Dictionary<string, HouseholdKeyEntryList>();


        [NotNull]
        private readonly Dictionary<string, ResultFileEntryLoader> _resultFileEntryLoaders = new Dictionary<string, ResultFileEntryLoader>();

        public CachingLPGProfileLoader([NotNull] ILogger myLogger, [NotNull] DBDto dbDto) : base(myLogger,
            Stage.ProfileGeneration,
            nameof(CachingLPGProfileLoader)) =>
            _dbDto = dbDto;

        [CanBeNull]
        public Profile LoadLPGProfile([NotNull] ProviderParameterDto parameters,
                                      [NotNull] string trafokreis,
                                      [NotNull] string loadtypetoSearchFor,
                                      [NotNull] SaveableEntry<Profile> saveableEntry,
                                      [NotNull] string householdKey,
                                      [NotNull] out string profileSource,
                                      [NotNull] string houseName,
                                      [NotNull] RunningConfig config,
                                      bool isInTestingMode = false)
        {
            if (saveableEntry.CheckForName(householdKey, MyLogger)) {
                List<Profile> cachedProfiles;
                try {
                    cachedProfiles = saveableEntry.LoadAllOrMatching("Name", householdKey);
                }
                catch (Exception) {
                    saveableEntry.DeleteEntryByName(householdKey);
                    throw;
                }

                if (cachedProfiles.Count != 1) {
                    Info("could not load profile from database");
                    profileSource = "missing file";
                    return null;
                }

                Profile cached = cachedProfiles[0];
                FileInfo cachedFile = new FileInfo(cached.SourceFileName ?? throw new InvalidOperationException());
                bool isFileCorrect = true;
                List<string> reasons = new List<string>();
                if (!cachedFile.Exists) {
                    isFileCorrect = false;
                    reasons.Add("File is missing:" + cachedFile.Name);
                }
                else {
                    if (cachedFile.LastWriteTime != cached.SourceFileDate) {
                        isFileCorrect = false;
                        reasons.Add("\nDate on the file has changed.: " + cachedFile.LastAccessTime + " vs. " + cached.SourceFileDate);
                    }

                    if (cachedFile.Length != cached.SourceFileLength) {
                        isFileCorrect = false;
                        reasons.Add("Size of the file has changed.");
                    }
                }

                if (cachedFile.DirectoryName?.ToLower().StartsWith(config.Directories.CalcServerLpgDirectory.ToLower()) != true) {
                    isFileCorrect = false;
                    reasons.Add("File is not in the calc server directory: " + config.Directories.CalcServerLpgDirectory + ", but instead in " +
                                cachedFile.DirectoryName);
                }

                if (!cachedFile.Name.Contains(loadtypetoSearchFor)) {
                    isFileCorrect = false;
                    reasons.Add("Filename does not contain the load type " + loadtypetoSearchFor + ", name is " + cachedFile.Name);
                }

                if (!cached.IsValidSingleYearProfile()) {
                    isFileCorrect = false;
                    reasons.Add("Not the right number of values in the profile");
                }

                if (isFileCorrect) {
                    if (reasons.Count > 0) {
                        throw new FlaException("Reasons for fail and still ok?");
                    }

                    profileSource = "LPG " + loadtypetoSearchFor + " (cached)";
                    Debug("using cached lpg profile for " + loadtypetoSearchFor);
                    return cached;
                }

                if (reasons.Count == 0) {
                    throw new FlaException("No reasons for fail, but not ok?");
                }

                string reason = string.Join("\n-", reasons);
                Debug("LPGImport: changed LPG file in source. Deleting cached version. Reason: " + reason);
                saveableEntry.DeleteEntryByName(householdKey);
            }

            // ReSharper disable once UseObjectOrCollectionInitializer
            string targetDir = Path.Combine(parameters.LPGDirectoryInfo, trafokreis, houseName);
            if (!Directory.Exists(targetDir)) {
                profileSource = "Targetdirectory missing, nothing loaded: " + targetDir;
                Debug("LPGImport: targetdirectory missing, skipping import: " + targetDir);
                return null;
            }

            var sqlitepath = Path.Combine(targetDir, "Results.General.sqlite");
            if (!File.Exists(sqlitepath)) {
                Debug("LPGImport: target sqlite missing, skipping import: " + targetDir);
                profileSource = "Results.General.Sqlite is missing";
                return null;
            }

            if (!_householdKeyEntries.ContainsKey(targetDir)) {
                _householdKeyEntries.Add(targetDir, HouseholdKeyEntryList.Load(targetDir));
            }

            var key = _householdKeyEntries[targetDir].FindHouseholdKeyByFlaHouseholdKey(householdKey, houseName, loadtypetoSearchFor);
            if (!_resultFileEntryLoaders.ContainsKey(targetDir)) {
                _resultFileEntryLoaders.Add(targetDir, ResultFileEntryLoader.Load(targetDir));
            }

            var jsonProfileFile = _resultFileEntryLoaders[targetDir].FindCorrectProfile(key.HouseholdKey, loadtypetoSearchFor);
            if (jsonProfileFile == null) {
                Debug("LPGImport: directory and sqlite found, but not json profile: " + targetDir);
                throw new HarmlessFlaException("no json profile was found in " + targetDir);
            }

            Info("Reading file " + jsonProfileFile + " in directory " + targetDir);
            Profile profile;
            FileInfo loadedFile;
            try {
                profile = ProfileLoader.LoadProfiles(jsonProfileFile, targetDir, out loadedFile);
                if (profile != null) {
                    if ((profile.Values.Count < 35000 || profile.Values.Count > 36000) && !isInTestingMode) {
                        loadedFile.Delete();
                        throw new FlaException("trying to read profile from profile " + loadedFile.FullName +
                                               ", but the number of values was wrong: Got " + profile.Values.Count +
                                               " values, instead of more than 35000, deleting file");
                    }
                }
            }
            catch (Exception ex) {
                throw new HarmlessFlaException("error while trying to read json profile: " + ex.Message);
            }

            if (profile == null) {
                throw new FlaException("Profile was null after loading. Loading from json failed.");
            }

            profile.Name = householdKey;
            profile.SourceFileName = loadedFile.FullName;
            profile.SourceFileLength = loadedFile.Length;
            profile.SourceFileDate = loadedFile.LastWriteTime;
            Debug("read newly calculated profile and updated the cache");
            saveableEntry.AddRow(profile);
            saveableEntry.SaveDictionaryToDatabase(MyLogger);
            profileSource = "LPG";
            return profile;
        }

        [CanBeNull]
        public Prosumer LoadProsumer([NotNull] ProviderParameterDto parameters,
                                     [NotNull] Hausanschluss ha,
                                     [NotNull] string prosumerName,
                                     HouseComponentType houseComponentType,
                                     [NotNull] string loadtypetoSearchFor,
                                     [NotNull] SaveableEntry<Profile> saveableEntry,
                                     [NotNull] string sourceGuid,
                                     [NotNull] string householdKey,
                                     [NotNull] string houseGuid,
                                     long isn,
                                     [NotNull] RunningConfig config)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            House house = _dbDto.Houses.FirstOrDefault(x => x.Guid == houseGuid);
            if (house == null) {
                throw new FlaException("Could not find house for " + houseGuid);
            }

            var profile = LoadLPGProfile(parameters,
                ha.Trafokreis,
                loadtypetoSearchFor,
                saveableEntry,
                householdKey,
                out var profileSource,
                house.ComplexName,
                config);
            if (profile == null) {
                return null;
            }

            Prosumer prosumer1 = new Prosumer(houseGuid,
                prosumerName,
                houseComponentType,
                sourceGuid,
                isn,
                ha.Guid,
                ha.ObjectID,
                GenerationOrLoad.Load,
                ha.Trafokreis,
                Name + " " + profileSource, "LPG " + profileSource ) {Profile = profile};
            return prosumer1;
        }

    }
}