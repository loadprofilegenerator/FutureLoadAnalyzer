using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Autofac;
using Automation;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    public class HouseProcessor : BasicLoggable {
        public enum ProcessingMode {
            Preparing,
            Collecting
        }

        [NotNull] private readonly string _processingResultPathForProfiles;

        [NotNull] private readonly ServiceRepository _services;

        public HouseProcessor([NotNull] ServiceRepository services, [NotNull] string processingResultPathForProfiles, Stage myStage) : base(
            services.Logger,
            myStage,
            nameof(HouseProcessor))
        {
            _services = services;
            _processingResultPathForProfiles = processingResultPathForProfiles;
        }

        [NotNull]
        public Prosumer GetSummedProsumer([NotNull] [ItemNotNull] List<Prosumer> prosumers, [NotNull] string trafokreis)
        {
            var p = Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour);
            foreach (var prosumer in prosumers) {
                Profile prof = prosumer.GetOrCreateNewProfile();
                if (prof.EnergyOrPower == EnergyOrPower.Power) {
                    prof = prof.ConvertFromPowerToEnergy();
                }

                p = p.Add(prof, "Sum");
            }

            var firstProsumer = prosumers.First();
            Prosumer newP = new Prosumer(firstProsumer.HouseGuid,
                firstProsumer.Name,
                HouseComponentType.Unknown,
                firstProsumer.SourceGuid,
                firstProsumer.Isn,
                firstProsumer.HausanschlussGuid,
                firstProsumer.HausanschlussKey,
                firstProsumer.GenerationOrLoad,
                trafokreis,
                "Summed Prosumer",
                "Sum");
            newP.TrafoKreis = trafokreis;
            newP.Profile = p;
            if (string.IsNullOrWhiteSpace(newP.TrafoKreis)) {
                throw new FlaException("Trafokreis for this prosumer was empty");
            }

            return newP;
        }

        public void ProcessAllHouses([NotNull] ScenarioSliceParameters parameters,
                                     [NotNull] Func<string, ScenarioSliceParameters, bool, string> makeAndRegisterFullFilename,
                                     ProcessingMode processingMode,
                                     [NotNull] [ItemNotNull] List<string> developmentStatus)
        {
            if (!Directory.Exists(_processingResultPathForProfiles)) {
                Directory.CreateDirectory(_processingResultPathForProfiles);
                Thread.Sleep(500);
            }
            else {
                var dstDi = new DirectoryInfo(_processingResultPathForProfiles);
                var files = dstDi.GetFiles();
                Info("Cleaning " + files.Length + " files from result directory " + _processingResultPathForProfiles);
                foreach (var file in files) {
                    file.Delete();
                }
            }

            var dbHouses = _services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, parameters);
            Info("using house db in  " + dbHouses.ConnectionString);
            var houses = dbHouses.Fetch<House>();
            HouseComponentRepository hcr = new HouseComponentRepository(dbHouses);
            var households = dbHouses.Fetch<Household>();
            var hausanschlusses = dbHouses.Fetch<Hausanschluss>();
            var cars = dbHouses.Fetch<Car>();
            var dbRaw = _services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var measuredRlmProfiles = dbRaw.Fetch<RlmProfile>();
            DBDto dbdto = new DBDto(houses, hausanschlusses, cars, households, measuredRlmProfiles);
            ProsumerComponentResultArchiver pra = new ProsumerComponentResultArchiver(Stage.ProfileGeneration, parameters, processingMode, _services);
            var trafokreiseToProcess = PrepareListOfTrafokreiseToProcess(hausanschlusses);

            List<HouseCreationAndCalculationJob> districts = new List<HouseCreationAndCalculationJob>();
            var vdewValues = dbRaw.Fetch<VDEWProfileValue>();
            var feiertage = dbRaw.Fetch<FeiertagImport>();
            var slpProvider = new SLPProvider(parameters.DstYear, vdewValues, feiertage);
            var loadProfileProviders = MakeDiContrainer(parameters, developmentStatus, hausanschlusses, houses, districts, slpProvider, dbdto);

            if (processingMode == ProcessingMode.Collecting) {
                ClearAllExistingExportProfiles();
            }

            var lpgDirectoryInfo = GetLPGCalcDirectoryInfo(parameters);
            ProfileGenerationRo pgRo = new ProfileGenerationRo();
            DateTime startTime = DateTime.Now;
            DateTime lastLog = DateTime.Now;
            Info("Processing " + houses.Count + " houses.");
            Info("Processing mode is " + processingMode);
            List<string> brokenLpgDirectories = new List<string>();
            List<string> brokenLpgJsons = new List<string>();
            Dictionary<string, List<HouseComponentEntry>> houseComponentsByObjectID = BuildDictionaryByObjektID(houses,
                hcr,
                hausanschlusses,
                pgRo,
                out var numberOfcomponents);
            int processedComponents = 0;
            Stopwatch swCollecting = new Stopwatch();
            Stopwatch swWriting = new Stopwatch();
            Dictionary<string, int> numberOfEmptyProsumers = new Dictionary<string, int>();
            HashSet<string> validHouseGuids = houses.Select(x => x.Guid).ToHashSet();
            foreach (KeyValuePair<string, List<HouseComponentEntry>> pair in houseComponentsByObjectID) {
                var houseProsumers = new List<Prosumer>();
                //erst alle profile einsammeln / vorbereiten
                swCollecting.Start();
                foreach (var component in pair.Value) {
                    processedComponents++;
                    if (processedComponents % 1000 == 0) {
                        Info(processingMode + " processed Components " + processedComponents + " / " + numberOfcomponents);
                    }

                    ProviderParameterDto ppdto = new ProviderParameterDto(component.Component, lpgDirectoryInfo.FullName, pgRo[component.Component]);
                    var provider = loadProfileProviders.GetCorrectProvider(component.Component);
                    pgRo[component.Component].UsedProvider = provider.Name;
                    if (processingMode == ProcessingMode.Preparing) {
                        provider.PrepareLoadProfileIfNeeded(ppdto);
                    }

                    if (processingMode == ProcessingMode.Collecting) {
                        if (trafokreiseToProcess.Contains(component.Hausanschluss.Trafokreis)) {
                            Prosumer p = provider.ProvideProfile(ppdto);
                            //Todo: add up profile per trafokreis, profile per provider etc right here
                            if (p != null) {
                                // some providers that are not ready will return null
                                if (p.Profile?.EnergyOrPower != EnergyOrPower.Energy) {
                                    throw new FlaException("Got a power profile from " + provider.Name);
                                }

                                CheckProfileIntegrity(p, provider, component.Component, validHouseGuids);
                                // ReSharper disable once AssignNullToNotNullAttribute
                                pgRo[component.Component].AddProsumerInformation(p);
                                houseProsumers.Add(p);
                                pra.Archive(p);
                            }
                            else {
                                if (!numberOfEmptyProsumers.ContainsKey(provider.Name)) {
                                    numberOfEmptyProsumers.Add(provider.Name, 0);
                                }

                                numberOfEmptyProsumers[provider.Name]++;
                            }
                        }
                    }
                }

                swCollecting.Stop();
                //dann alles rausschreiben
                swWriting.Start();
                if (processingMode == ProcessingMode.Collecting) {
                    var generationProsumers = houseProsumers.Where(x => x.GenerationOrLoad == GenerationOrLoad.Generation).ToList();
                    var loadProsumers = houseProsumers.Where(x => x.GenerationOrLoad == GenerationOrLoad.Load).ToList();
                    var component = pair.Value[0];
                    if (loadProsumers.Count > 0) {
                        Prosumer summedProsumer = GetSummedProsumer(loadProsumers, component.Hausanschluss.Trafokreis);
                        var haros = pgRo.HausanschlussByObjectId(pair.Key);
                        double maxPower = summedProsumer.Profile?.MaxPower() ?? 0;
                        foreach (var haro in haros) {
                            haro.MaximumPower = maxPower;
                        }

                        WriteSumLineToCsv(summedProsumer, component.Hausanschluss.Trafokreis, GenerationOrLoad.Load);
                    }

                    if (generationProsumers.Count > 0) {
                        var sumProfile = GetSummedProsumer(generationProsumers, component.Hausanschluss.Trafokreis);
                        WriteSumLineToCsv(sumProfile, component.Hausanschluss.Trafokreis, GenerationOrLoad.Generation);
                        // ReSharper disable once PossibleNullReferenceException
                    }
                }

                swWriting.Stop();
                ReportProgress(startTime, processedComponents, numberOfcomponents, parameters, ref lastLog, swCollecting, swWriting);
            }

            Info("Finished processing all components, finishing up now. Duration: " + (DateTime.Now - startTime).TotalMinutes.ToString("F2") +
                 " minutes");
            Info("collecting took " + swCollecting.Elapsed + " and writing took " + swWriting.Elapsed);
            foreach (var pair in numberOfEmptyProsumers) {
                Info("NumberOfEmptyProsumers for " + pair.Key + " was " + pair.Value);
            }

            if (processingMode == ProcessingMode.Preparing) {
                DateTime endtime = new DateTime(parameters.DstYear, 12, 31);
                string houseJobDirectory = Path.Combine(_services.RunningConfig.Directories.HouseJobsDirectory,
                    parameters.DstScenario.ToString(),
                    parameters.DstYear.ToString());
                DirectoryInfo houseJobDi = new DirectoryInfo(houseJobDirectory);
                WriteDistrictsForLPG(districts, houseJobDi, _services.Logger, parameters, endtime, pgRo);
            }
            else {
                DateTime startSavingToDB = DateTime.Now;
                pra.FinishSavingEverything();
                Info("Finished writing prosumers to db. Duration: " + (DateTime.Now - startSavingToDB).TotalMinutes.ToString("F2") + " minutes");
            }

            pra.Dispose();
            var excelFiles = WriteExcelResultFiles(parameters, makeAndRegisterFullFilename, processingMode, pgRo);
            if (_services.RunningConfig.CollectFilesForArchive) {
                SaveToArchiveDirectory(parameters, excelFiles, _services.StartingTime, _services.RunningConfig);
            }

            WriteBrokenLpgCalcCleanupBatch(parameters, makeAndRegisterFullFilename, processingMode, brokenLpgDirectories, brokenLpgJsons);
            if (processingMode == ProcessingMode.Collecting) {
                foreach (var provider in loadProfileProviders.Providers) {
                    provider.DoFinishCheck();
                }
            }
        }

        public static void WriteDistrictsForLPG([NotNull] [ItemNotNull] List<HouseCreationAndCalculationJob> houses,
                                                [NotNull] DirectoryInfo di,
                                                [NotNull] ILogger logger,
                                                [NotNull] ScenarioSliceParameters slice,
                                                DateTime endDateForCalc,
                                                [NotNull] ProfileGenerationRo pgRo)
        {
            var calcSpec = MakeCalcSpec(slice, endDateForCalc);

            //first write all the districts
            logger.Info("starting to write the lpg housejobs", Stage.ProfileGeneration, nameof(HouseProcessor));
            //next write the filtered districts
            var directory = Path.Combine(di.FullName, "Districts");
            var filteredHouses = new List<HouseCreationAndCalculationJob>();
            int skippedCount = 0;
            foreach (HouseCreationAndCalculationJob houseJob in houses) {
                //filter houses that don't need to calculated
                if (houseJob.House.Households.Count == 0) {
                    continue;
                }

                bool allHouseholdsFound = true;
                bool allCarsFound = true;
                List<HouseholdData> missingHouseholds = new List<HouseholdData>();
                List<HouseholdData> missingCars = new List<HouseholdData>();

                foreach (HouseholdData householdJob in houseJob.House.Households) {
                    if (!householdJob.IsHouseholdProfileCalculated) {
                        allHouseholdsFound = false;
                        missingHouseholds.Add(householdJob);
                    }

                    if (!householdJob.IsCarProfileCalculated && householdJob.UseElectricCar == ElectricCarUse.UseElectricCar) {
                        allCarsFound = false;
                        missingCars.Add(householdJob);
                    }
                }

                var houseRo = pgRo.FindByHousename(houseJob.House.Name);
                if (allHouseholdsFound && allCarsFound) {
                    skippedCount++;
                    houseRo.LpgCalculationStatus = "All Calculations Finished";
                    continue;
                }

                var householdsWithCars = houseJob.House.Households.Where(x => x.UseElectricCar == ElectricCarUse.UseElectricCar).ToList();
                houseRo.LpgCalculationStatus = "Incomplete, " + missingHouseholds.Count + "/" + houseJob.House.Households.Count +
                                               " households missing, " + +missingCars.Count + " / " + householdsWithCars.Count + " cars missing";
                filteredHouses.Add(houseJob);
            }

            logger.Info("Skipping " + skippedCount + " houses due to complete results in cache", Stage.ProfileGeneration, nameof(HouseProcessor));
            WriteHousejobsToDirectory(directory, filteredHouses, calcSpec, logger);
        }

        [NotNull]
        private Dictionary<string, List<HouseComponentEntry>> BuildDictionaryByObjektID([NotNull] [ItemNotNull] List<House> houses,
                                                                                        [NotNull] HouseComponentRepository hcr,
                                                                                        [NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses,
                                                                                        [NotNull] ProfileGenerationRo pgRo,
                                                                                        out int numberOfcomponents)
        {
            var houseComponentsByObjectID = new Dictionary<string, List<HouseComponentEntry>>();
            numberOfcomponents = 0;
            int housecount = 0;
            foreach (House house in houses) {
                pgRo.AddHouse(house);
                var houseComponents = house.CollectHouseComponents(hcr);
                pgRo[house].NumberOfComponents = houseComponents.Count;
                var hausanschlussGuids = houseComponents.Where(x => x.HausAnschlussGuid != null).Select(x => x.HausAnschlussGuid).Distinct().ToList();

                foreach (string hausanschlussGuid in hausanschlussGuids) {
                    var hausanschluss = hausanschlusses.First(x => x.Guid == hausanschlussGuid);

                    pgRo.AddHausanschluss(house, hausanschluss, "Mit Komponenten");
                    var anschlusshouseComponents = houseComponents.Where(x => x.HausAnschlussGuid == hausanschlussGuid).ToList();

                    if (!houseComponentsByObjectID.ContainsKey(hausanschluss.ObjectID)) {
                        houseComponentsByObjectID.Add(hausanschluss.ObjectID, new List<HouseComponentEntry>());
                    }

                    foreach (IHouseComponent component in anschlusshouseComponents) {
                        houseComponentsByObjectID[hausanschluss.ObjectID].Add(new HouseComponentEntry(component, hausanschluss, house));
                        numberOfcomponents++;
                        pgRo.AddHouseComponent(house, hausanschluss, component);
                    }
                }

                foreach (var hausanschluss in house.Hausanschluss) {
                    if (hausanschlussGuids.Contains(hausanschluss.Guid)) {
                        continue;
                    }

                    pgRo.AddHausanschluss(house, hausanschluss, "Ohne Komponenten");
                }

                housecount++;
                if (_services.RunningConfig.LimitLoadProfileGenerationToHouses > 0) {
                    if (housecount > _services.RunningConfig.LimitLoadProfileGenerationToHouses) {
                        break;
                    }
                }
            }

            return houseComponentsByObjectID;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void CheckProfileIntegrity([CanBeNull] Prosumer p,
                                                  [NotNull] ILoadProfileProvider provider,
                                                  [NotNull] IHouseComponent consumer,
                                                  [NotNull] [ItemNotNull] HashSet<string> validHouseGuids)
        {
            if (p == null) {
                throw new FlaException("Provider  " + provider.Name + " returned null");
            }

            if (!validHouseGuids.Contains(p.HouseGuid)) {
                throw new FlaException("Prosumer returned and invalid house guid");
            }

            var profile = p.Profile;
            if (profile == null) {
                throw new FlaException("Invalid profile generation in provider " + provider.Name + ". Profile was null. House component was : " +
                                       consumer.HouseComponentType);
            }

            if (consumer.EffectiveEnergyDemand > 0 && Math.Abs(consumer.EffectiveEnergyDemand) < 0.00001) {
                throw new FlaException("Energy demand was not correctly initalized for " + consumer.Name + " (" + consumer.GetType().FullName + ")");
            }

            if (profile.Values.Count != 35040) {
                throw new FlaException("Not a yearly profile form provider " + provider.Name + ". Profile count was " + profile.Values.Count);
            }

            if (profile.Values.Any(x => double.IsNaN(x))) {
                throw new FlaException("Profile contained NAN from provider " + provider.Name + " for customer " + consumer.Name);
            }

            if (p.TrafoKreis == null) {
                throw new FlaException("Trafokreis not set by " + provider.Name + " for customer " + consumer.Name);
            }

            if (p.ProviderName == null) {
                throw new FlaException("Trafokreis not set by " + provider.Name + " for customer " + consumer.Name);
            }
        }

        private void ClearAllExistingExportProfiles()
        {
            if (Directory.Exists(_processingResultPathForProfiles)) {
                Directory.Delete(_processingResultPathForProfiles, true);
            }

            Directory.CreateDirectory(_processingResultPathForProfiles);
            Thread.Sleep(250);
        }

        [NotNull]
        private DirectoryInfo GetLPGCalcDirectoryInfo([NotNull] ScenarioSliceParameters parameters)
        {
            var di = new DirectoryInfo(Path.Combine(_services.RunningConfig.Directories.CalcServerLpgDirectory,
                parameters.DstScenario.ToString(),
                parameters.DstYear.ToString()));
            if (!di.Exists) {
                di.Create();
                Thread.Sleep(250);
            }

            return di;
        }

        [NotNull]
        private static JsonCalcSpecification MakeCalcSpec([NotNull] ScenarioSliceParameters parameters, DateTime endtime)
        {
            DateTime startTime = new DateTime(parameters.DstYear, 1, 1);
            JsonReference geoloc = new JsonReference("(Germany) Chemnitz", "eddeb22c-fbd4-44c1-bf2d-fbde3342f1bd");
            JsonReference tempReference = new JsonReference("Berlin, Germany 1996 from Deutscher Wetterdienst DWD (www.dwd.de)",
                "ec337ba6-60a1-404b-9db0-9be52c9e5702");
            JsonCalcSpecification jcs = new JsonCalcSpecification(null,
                null,
                false,
                null,
                false,
                endtime,
                "00:15:00",
                "00:01:00",
                geoloc,
                LoadTypePriority.RecommendedForHouses,
                null,
                false,
                startTime,
                tempReference,
                null,
                null) {
                DeleteDAT = true,
                DefaultForOutputFiles = OutputFileDefault.None, SkipExisting = true
            };
            jcs.CalcOptions.Add(CalcOption.SumProfileExternalIndividualHouseholdsAsJson);
            jcs.LoadtypesForPostprocessing.Add("Electricity");
            jcs.LoadtypesForPostprocessing.Add("Car Charging Electricity");
            jcs.OutputDirectory = "Results";
            jcs.IgnorePreviousActivitiesWhenNeeded = true;
            return jcs;
        }

        [NotNull]
        private ProviderCollection MakeDiContrainer([NotNull] ScenarioSliceParameters parameters,
                                                    [NotNull] [ItemNotNull] List<string> developmentStatus,
                                                    [NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses,
                                                    [NotNull] [ItemNotNull] List<House> houses,
                                                    [NotNull] [ItemNotNull] List<HouseCreationAndCalculationJob> districts,
                                                    [NotNull] SLPProvider slpProvider,
                                                    [NotNull] DBDto dbdto)
        {
            ContainerBuilder builder = new ContainerBuilder();
            var mainAssembly = Assembly.GetAssembly(typeof(MainBurgdorfStatisticsCreator));
            builder.RegisterAssemblyTypes(mainAssembly).Where(t => t.GetInterfaces().Contains(typeof(ILoadProfileProvider))).AsSelf()
                .As<ILoadProfileProvider>();
            builder.RegisterType<ProviderCollection>().SingleInstance();
            builder.RegisterType<CachingLPGProfileLoader>().SingleInstance();
            builder.Register(x => _services.Logger).As<ILogger>();
            builder.Register(x => _services).As<ServiceRepository>();
            builder.Register(x => hausanschlusses).SingleInstance();
            builder.Register(x => houses).SingleInstance();
            builder.Register(x => districts).SingleInstance();
            builder.Register(x => parameters).SingleInstance();
            builder.Register(x => slpProvider).SingleInstance();
            builder.Register(x => dbdto).SingleInstance();
            var container = builder.Build();
            ProviderCollection loadProfileProviders = container.Resolve<ProviderCollection>();
            foreach (var provider in loadProfileProviders.Providers) {
                foreach (var dev in provider.DevelopmentStatus) {
                    developmentStatus.Add(provider.Name + ": " + dev);
                }
            }

            return loadProfileProviders;
        }

        [NotNull]
        private static DirectoryInfo PrepareArchiveDir([NotNull] string resultArchiveDirectory)
        {
            DirectoryInfo archiveDir = new DirectoryInfo(resultArchiveDirectory);

            var dstFiles = archiveDir.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var info in dstFiles) {
                try {
                    info.Delete();
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    SLogger.Error(ex.Message);
                }
            }

            return archiveDir;
        }

        [NotNull]
        [ItemNotNull]
        private List<string> PrepareListOfTrafokreiseToProcess([NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusses)
        {
            List<string> trafokreiseToProcess;
            if (_services.RunningConfig.NumberOfTrafokreiseToProcess != -1) {
                trafokreiseToProcess = hausanschlusses.Select(x => x.Trafokreis).Distinct().Take(_services.RunningConfig.NumberOfTrafokreiseToProcess)
                    .ToList();
            }
            else {
                trafokreiseToProcess = hausanschlusses.Select(x => x.Trafokreis).Distinct().ToList();
            }

            return trafokreiseToProcess;
        }

        private void ReportProgress(DateTime start,
                                    int processedHouseCount,
                                    int totalNumberOfComponents,
                                    [NotNull] ScenarioSliceParameters slice,
                                    ref DateTime lastLog,
                                    [NotNull] Stopwatch collecting,
                                    [NotNull] Stopwatch writing)
        {
            TimeSpan durationsinceLastLog = DateTime.Now - lastLog;
            if (durationsinceLastLog.TotalSeconds < 10) {
                return;
            }

            lastLog = DateTime.Now;
            TimeSpan duration = DateTime.Now - start;
            double average = processedHouseCount / duration.TotalSeconds;
            double timeLeft = (totalNumberOfComponents - processedHouseCount) / average / 60;
            _services.Logger.Info(slice + ": processed " + processedHouseCount + "/" + totalNumberOfComponents + " components in " +
                                  duration.TotalSeconds + "s, for a rate of " + average + " per second, estimated time left: " +
                                  timeLeft.ToString("F1") + " minutes, collecting took " + collecting.Elapsed + ", writing took " + writing.Elapsed,
                Stage.ProfileGeneration,
                "HouseProcessor");
        }

        private void SaveToArchiveDirectory([NotNull] ScenarioSliceParameters parameters,
                                            [NotNull] [ItemNotNull] List<string> excelFiles,
                                            DateTime startingTime,
                                            [NotNull] RunningConfig config)
        {
            DirectoryInfo srcDir = new DirectoryInfo(_processingResultPathForProfiles);
            {
                var loadDirName = BasicRunnable.GetResultArchiveDirectory(parameters, startingTime, config, RelativeDirectory.Load, null);
                var loadDir = PrepareArchiveDir(loadDirName);
                var loadFiles = srcDir.GetFiles("*.load.csv");
                foreach (var loadFile in loadFiles) {
                    string dstName = Path.Combine(loadDir.FullName, loadFile.Name);
                    Info("Copying to " + dstName);
                    loadFile.CopyTo(dstName, true);
                }
            }

            {
                var genDirName = BasicRunnable.GetResultArchiveDirectory(parameters, startingTime, config, RelativeDirectory.Generation, null);
                var genDir = PrepareArchiveDir(genDirName);
                var genFiles = srcDir.GetFiles("*.Generation.csv");
                foreach (var genFile in genFiles) {
                    string dstName = Path.Combine(genDir.FullName, genFile.Name);
                    Info("Copying to " + dstName);
                    genFile.CopyTo(dstName, true);
                }
            }

            {
                var genDirName = BasicRunnable.GetResultArchiveDirectory(parameters, startingTime, config, RelativeDirectory.Report, null);
                var genDir = PrepareArchiveDir(genDirName);
                foreach (var file in excelFiles) {
                    FileInfo fi1 = new FileInfo(file);
                    fi1.CopyTo(Path.Combine(genDir.FullName, fi1.Name));
                }
            }
        }

        private static void WriteBrokenLpgCalcCleanupBatch([NotNull] ScenarioSliceParameters parameters,
                                                           [NotNull] Func<string, ScenarioSliceParameters, bool, string> makeAndRegisterFullFilename,
                                                           ProcessingMode processingMode,
                                                           [NotNull] [ItemNotNull] List<string> brokenLpgDirectories,
                                                           [NotNull] [ItemNotNull] List<string> brokenLpgJsons)
        {
            var lpgCleaner = makeAndRegisterFullFilename("CleanBrokenLPGStuff." + processingMode + "." + parameters.GetFileName() + ".cmd",
                parameters,
                true);
            Encoding utf8WithoutBom = new UTF8Encoding(false);
            FileStream fs = new FileStream(lpgCleaner, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, utf8WithoutBom);
            sw.WriteLine("chcp 65001");
            foreach (string brokenLpgDirectory in brokenLpgDirectories) {
                sw.WriteLine("rmdir /S /Q \"" + brokenLpgDirectory + "\" ");
            }

            foreach (string brokenLpgJson in brokenLpgJsons) {
                sw.WriteLine("del \"" + brokenLpgJson + "\" ");
            }

            sw.Close();
        }

        [NotNull]
        [ItemNotNull]
        private static List<string> WriteExcelResultFiles([NotNull] ScenarioSliceParameters parameters,
                                                          [NotNull] Func<string, ScenarioSliceParameters, bool, string> makeAndRegisterFullFilename,
                                                          ProcessingMode processingMode,
                                                          [NotNull] ProfileGenerationRo pgRo)
        {
            List<string> excelFileNames = new List<string>();
            var fn1 = makeAndRegisterFullFilename("AllGeneratedLoadProfilesAndEnergy." + processingMode + ".Tree.xlsx", parameters, true);
            excelFileNames.Add(fn1);
            pgRo.DumpToExcel(fn1, XlsResultOutputMode.Tree);
            var fn2 = makeAndRegisterFullFilename("AllGeneratedLoadProfilesAndEnergy." + processingMode + ".Full.xlsx", parameters, true);
            pgRo.DumpToExcel(fn2, XlsResultOutputMode.FullLine);
            excelFileNames.Add(fn2);
            var fn3 = makeAndRegisterFullFilename("AllGeneratedLoadProfilesAndEnergy." + processingMode + ".ByTrafoStationTree.xlsx",
                parameters,
                true);
            excelFileNames.Add(fn3);
            pgRo.DumpToExcel(fn3, XlsResultOutputMode.ByTrafoStationTree);

            var fn4 = makeAndRegisterFullFilename("AllGeneratedLoadProfilesAndEnergy." + processingMode + ".ByTrafoStationHausanschlussTree.xlsx",
                parameters,
                true);
            excelFileNames.Add(fn4);
            pgRo.DumpToExcel(fn4, XlsResultOutputMode.ByTrafoStationHausanschlussTree);
            return excelFileNames;
        }

        private static void WriteHousejobsToDirectory([NotNull] string directory,
                                                      [NotNull] [ItemNotNull] List<HouseCreationAndCalculationJob> filteredHouses,
                                                      [NotNull] JsonCalcSpecification calcSpec,
                                                      [NotNull] ILogger logger)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            DirectoryInfo subdir = new DirectoryInfo(directory);
            if (!subdir.Exists) {
                subdir.Create();
                Thread.Sleep(250);
            }

            var files = subdir.GetFiles("*.json");
            foreach (var file in files) {
                file.Delete();
            }

            int housejobCount = 0;
            foreach (HouseCreationAndCalculationJob houseJob in filteredHouses) {
                foreach (var hhj in houseJob.House.Households) {
                    if (hhj.HouseholdDataPersonSpecification.Persons.Count == 0) {
                        throw new FlaException("No persons were defined?");
                    }
                }

                houseJob.CalcSpec = calcSpec;
                var filename = Path.Combine(directory, "HouseJob." + houseJob.House.Name + ".json");
                var sw = new StreamWriter(filename);
                sw.WriteLine(JsonConvert.SerializeObject(houseJob, Formatting.Indented));
                sw.Close();
                housejobCount++;
            }

            stopwatch.Stop();
            logger.Info("wrote " + housejobCount + " house jobs to disc in " + stopwatch.Elapsed.TotalSeconds.ToString("F1") + " seconds",
                Stage.ProfileGeneration,
                nameof(HouseProcessor));
        }

        private void WriteSumLineToCsv([NotNull] Prosumer p, [NotNull] string trafokreis, GenerationOrLoad generationOrLoad)
        {
            string tkFileName = FilenameHelpers.CleanFileName(trafokreis);
            var csvFileNameGeneration = Path.Combine(_processingResultPathForProfiles, tkFileName + "." + generationOrLoad + ".csv");
            StreamWriter sw = new StreamWriter(csvFileNameGeneration, true);
            sw.WriteLine(p.GetCSVLine());
            sw.Close();
        }

        private class HouseComponentEntry {
            public HouseComponentEntry([NotNull] IHouseComponent component, [NotNull] Hausanschluss hausanschluss, [NotNull] House house)
            {
                Component = component;
                Hausanschluss = hausanschluss;
                House = house;
            }

            [NotNull]
            public IHouseComponent Component { get; }

            [NotNull]
            public Hausanschluss Hausanschluss { get; }

            [NotNull]
            public House House { [UsedImplicitly] get; }
        }
    }
}