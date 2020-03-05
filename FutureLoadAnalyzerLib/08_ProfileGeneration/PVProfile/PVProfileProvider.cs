using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.SAM;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    // ReSharper disable once InconsistentNaming
    public class PVProfileProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] private readonly DBDto _dbDto;
        [NotNull] [ItemNotNull] private readonly HashSet<string> _checkedKeys = new HashSet<string>();
        [NotNull] private readonly SaveableEntry<Profile> _saveableEntries;

        public PVProfileProvider([NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice,
                                 [NotNull] DBDto dbDto) : base(nameof(PVProfileProvider), services, slice)
        {
            _dbDto = dbDto;
            DevelopmentStatus.Add("use the correct weather year profile for the generation");
            DevelopmentStatus.Add("instead of commenting out, check if all angles & right weather file based on key. If not right, clear table and regenerate");
            Directory.SetCurrentDirectory(Services.RunningConfig.Directories.SamDirectory);
            Info("SSC Version number = " + API.Version());
            Info("SSC bBuild Information = " + API.BuildInfo());
            var dbPV = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, Slice, DatabaseCode.PvProfiles);
            _saveableEntries = SaveableEntry<Profile>.GetSaveableEntry(dbPV, SaveableEntryTableType.PVGeneration, MyLogger);
            _saveableEntries.MakeTableForListOfFieldsIfNotExists(true);
        }

        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Photovoltaik) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters)
        {
            Module.SetPrint(0);
            //var relevantPotentials = _pvPotentials.Where(x => x.HouseGuid == houseComponent.HouseGuid);
            PvSystemEntry entry = (PvSystemEntry)parameters.HouseComponent;
            int idx = 0;
            foreach (var area in entry.PVAreas) {
                var key = MakeKeyFromPVArea(area);
                var keystr = key.GetKey();
                //key has been checked in this run
                if (_checkedKeys.Contains(keystr)) {
                    continue;
                }

                _checkedKeys.Add(keystr);
                bool isInDb = _saveableEntries.CheckForName(keystr, MyLogger);
                if (isInDb) {
                    continue;
                }

                Info("Missing pv profile for " + keystr + ", generating...");
                PVSystemSettings pvs = new PVSystemSettings(key, 1, 1, MyLogger, idx++);
                var profile = pvs.Run(Services.RunningConfig);
                _saveableEntries.AddRow(profile);
                _saveableEntries.SaveDictionaryToDatabase(MyLogger);
            }

            return true;
        }

        [NotNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            Module.SetPrint(0);
            //var relevantPotentials = _pvPotentials.Where(x => x.HouseGuid == houseComponent.HouseGuid);
            PvSystemEntry entry = (PvSystemEntry)ppdto.HouseComponent;
            if (_dbDto.Hausanschlusse  == null) {
                throw new FlaException("hausanschlüsse were not initalized");
            }
            if (_dbDto.Hausanschlusse.Count == 0) {
                throw new FlaException("not a single hausanschluss");
            }

            Hausanschluss hausanschluss = _dbDto.Hausanschlusse.FirstOrDefault(x => x.Guid == entry.HausAnschlussGuid);
            if (hausanschluss == null) {
                throw new FlaException("No hausanschluss found for guid: " + entry.HausAnschlussGuid);
            }

            if (hausanschluss.ObjectID.ToLower().Contains("leuchte")) {
                throw new FlaException("PV anlage an einer leuchte! " + hausanschluss.ObjectID + " - " + entry.Name  );
            }
            //TODO: change this to use pv system areas from the pvsystem entry
            Profile sumProf = Profile.MakeConstantProfile(0, ppdto.HouseComponent.Name, Profile.ProfileResolution.QuarterHour);
            if (Math.Abs(entry.PVAreas.Sum(x => x.Energy) - entry.EffectiveEnergyDemand) > 0.1) {
                throw new FlaException("Sum of the pv areas did not match pv entry sum");
            }
            foreach (var area in entry.PVAreas) {
                var key = MakeKeyFromPVArea(area);
                var keystr = key.GetKey();
                var areaProfiles = _saveableEntries.LoadAllOrMatching("Name", keystr);
                if (areaProfiles.Count != 1) {
                    throw new FlaException("Invalid count");
                }

                var areaProfile = areaProfiles[0];
                areaProfile.EnergyOrPower = EnergyOrPower.Energy;
                areaProfile = areaProfile.ScaleToTargetSum(area.Energy, entry.Name, out var _);
                sumProf = sumProf.Add(areaProfile, entry.Name);
            }

            if (Math.Abs(Slice.PVCurtailToXPercent) < 0.00001) {
                throw new FlaException("Found curtailment to 0");
            }

            if (sumProf.EnergySum() < 0) {
                throw new FlaException("Negative PV Power");
            }

            if (Slice.PVCurtailToXPercent < 1) {
                sumProf = sumProf.LimitPositiveToPercentageOfMax(Slice.PVCurtailToXPercent);
            }

            var prosumer = new Prosumer(entry.HouseGuid, entry.Name,
                HouseComponentType.Photovoltaik, entry.SourceGuid, entry.FinalIsn, entry.HausAnschlussGuid, hausanschluss.ObjectID,
                GenerationOrLoad.Generation,
                hausanschluss.Trafokreis,Name, "PV Profile") {Profile = sumProf};
            if ( Math.Abs(Slice.PVCurtailToXPercent - 1) < 0.01 && Math.Abs(sumProf.EnergySum() - entry.EffectiveEnergyDemand) > 0.1 ) {
                throw new FlaException("PV Energy result is wrong");
            }
            return prosumer;
        }

        private PVSystemKey MakeKeyFromPVArea([NotNull] PVSystemArea area)
        {
            PVSystemKey key = new PVSystemKey((int)area.Azimut, (int)area.Tilt, Slice.DstYear);
            return key;
        }
    }
}