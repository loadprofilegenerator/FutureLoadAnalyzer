using System;
using System.Collections.Generic;
using System.Linq;
using Automation;
using Common;
using Common.Config;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Gender = Automation.Gender;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    /// <summary>
    ///     make the household profiles
    /// </summary>
// ReSharper disable once InconsistentNaming
    public class HouseholdLoadProfileProvider : BaseProvider, ILoadProfileProvider {
        private const string LoadtypetoSearchFor = "Electricity";
        [NotNull] private readonly DBDto _dbDto;
        [NotNull] [ItemNotNull] private readonly List<HouseCreationAndCalculationJob> _housesToBeCreated;
        [NotNull] private readonly CachingLPGProfileLoader _lpgloader;


        [NotNull] private readonly SaveableEntry<Profile> _saveableEntry;
        [NotNull] private readonly SLPProvider _slpProvider;

        public HouseholdLoadProfileProvider([NotNull] ServiceRepository services,
                                            [NotNull] ScenarioSliceParameters slice,
                                            [NotNull] [ItemNotNull] List<HouseCreationAndCalculationJob> districtsToBeCreated,
                                            [NotNull] SLPProvider slpProvider,
                                            [NotNull] DBDto dbDto,
                                            [NotNull] CachingLPGProfileLoader lpgloader) : base(nameof(HouseholdLoadProfileProvider), services, slice)
        {
            _housesToBeCreated = districtsToBeCreated ?? throw new ArgumentNullException(nameof(districtsToBeCreated));
            _slpProvider = slpProvider;
            _dbDto = dbDto;
            _lpgloader = lpgloader;
            var profileCacheDb = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.HouseholdProfiles);
            _saveableEntry = SaveableEntry<Profile>.GetSaveableEntry(profileCacheDb, SaveableEntryTableType.LPGProfile, services.Logger);
            _saveableEntry.MakeTableForListOfFieldsIfNotExists(true);
        }

        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Household) {
                return true;
            }

            return false;
        }


        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters)
        {
            if (parameters.HouseComponent.HouseComponentType != HouseComponentType.Household) {
                throw new FlaException("Was not household: " + parameters.HouseComponent.HouseComponentType);
            }

            Household hh = (Household)parameters.HouseComponent;
            Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == hh.HausAnschlussGuid);
            var houseJob = _housesToBeCreated.FirstOrDefault(x => x.House.HouseGuid == hh.HouseGuid);
            if (houseJob == null) {
                var flahouse = _dbDto.Houses.Single(x => x.Guid == hh.HouseGuid);
                houseJob = new HouseCreationAndCalculationJob(Slice.DstScenario.ToString(), Slice.DstYear.ToString(), ha.Trafokreis);
                houseJob.House = new HouseData(hh.HouseGuid, "HT01", 0, 0, flahouse.ComplexName);
                _housesToBeCreated.Add(houseJob);
            }

            HouseholdData hd = new HouseholdData(hh.HouseholdKey,
                hh.EffectiveEnergyDemand,
                ElectricCarUse.NoElectricCar,
                hh.Name,
                null,
                null,
                null,
                null,
                HouseholdDataSpecifictionType.ByPersons);
            hd.IsCarProfileCalculated = true;
            hd.HouseholdDataPersonSpecification = new HouseholdDataPersonSpecification(new List<PersonData>());
            if (hh.Occupants.Count == 0) {
                throw new FlaException("No occupants in the household " + hh.Name);
            }

            foreach (var occupant in hh.Occupants) {
                hd.HouseholdDataPersonSpecification.Persons.Add(new PersonData(occupant.Age, (Gender)occupant.Gender));
            }

            House house = _dbDto.Houses.First(x => x.Guid == hh.HouseGuid);
            if (Services.RunningConfig.LpgPrepareMode == LpgPrepareMode.PrepareWithFullLpgLoad) {
                Profile lpgProfile = null;
                try {
                    lpgProfile = _lpgloader.LoadLPGProfile(parameters,
                        ha.Trafokreis,
                        LoadtypetoSearchFor,
                        _saveableEntry,
                        hh.HouseholdKey,
                        out _,
                        house.ComplexName,
                        Services.RunningConfig);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    parameters.HouseComponentResultObject.LPGErrors = "trying to load lpg profile failed: " + ex.Message;
                    Error("trying to load lpg profile: " + ex.Message);
                }

                if (lpgProfile != null) {
                    hd.IsHouseholdProfileCalculated = true;
                }
                else {
                    hd.IsHouseholdProfileCalculated = false;
                }
            }
            else if (Services.RunningConfig.LpgPrepareMode == LpgPrepareMode.PrepareWithOnlyNamecheck) {
                if (_saveableEntry.CheckForName(hh.HouseholdKey, Services.Logger)) {
                    hd.IsHouseholdProfileCalculated = true;
                }
                else {
                    hd.IsHouseholdProfileCalculated = false;
                }
            }
            else {
                throw new FlaException("Unknown lpg prepare mode");
            }

            houseJob.House.Households.Add(hd);
            return true;
        }

        [NotNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            Prosumer prosumer;
            var ha = _dbDto.Hausanschlusse.Single(x => x.Guid == ppdto.HouseComponent.HausAnschlussGuid);

            var household = (Household)ppdto.HouseComponent;
            if (Math.Abs(household.EffectiveEnergyDemand) < 0.00001) {
                throw new FlaException("Household without energy demand: " + household.Name);
            }

            if (Services.RunningConfig.CheckForLpgCalcResult) {
                try {
                    prosumer = ProvideLPGProfile(ppdto, ha, household);

                    if (prosumer != null && prosumer.Profile != null) {
                        if (Math.Abs(prosumer.Profile.EnergySum() - household.EffectiveEnergyDemand) > 0.000001) {
                            prosumer.Profile = prosumer.Profile.ScaleToTargetSum(household.EffectiveEnergyDemand,
                                prosumer.Profile.Name,
                                out var factor);
                            ppdto.HouseComponentResultObject.AdjustmentFactor = factor;
                            if (factor > 2) {
                                throw new HarmlessFlaException("LPG Scaling factor > 2");
                            }

                            if (prosumer.Profile.Values.Max() > 2.5) {
                                throw new HarmlessFlaException("LPG Peak Load was over 10 kW");
                            }
                        }

                        ppdto.HouseComponentResultObject.ProcessingStatus = "LPG Profile";
                        return prosumer;
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Error(ex.Message);
                    ppdto.HouseComponentResultObject.ErrorMessage = ex.Message;
                }
            }

            prosumer = MakeSlpProfile(household, ha);
            ppdto.HouseComponentResultObject.ProcessingStatus = "H0 Profile";
            return prosumer;
        }

        [NotNull]
        private Prosumer MakeSlpProfile([NotNull] Household household, [NotNull] Hausanschluss ha)
        {
            var slpprofile = _slpProvider.Run("H0", household.EffectiveEnergyDemand);
            if (slpprofile == null) {
                throw new FlaException("No profile? Why?");
            }

            var prosumer = new Prosumer(household.HouseGuid,
                household.Name,
                HouseComponentType.Household,
                household.SourceGuid,
                household.FinalIsn,
                household.HausAnschlussGuid,
                ha.ObjectID,
                GenerationOrLoad.Load,
                ha.Trafokreis,
                Name + " - SLP H0 Substitute Profile",
                "H0") {Profile = slpprofile};
            return prosumer;
        }

        [CanBeNull]
        private Prosumer ProvideLPGProfile([NotNull] ProviderParameterDto parameters, [NotNull] Hausanschluss ha, [NotNull] Household household)
        {
            string prosumerName = household.Name;
            const HouseComponentType houseComponentType = HouseComponentType.Household;
            string sourceGuid = household.SourceGuid;
            string householdKey = household.HouseholdKey;
            long isn = household.FinalIsn;

            return _lpgloader.LoadProsumer(parameters,
                ha,
                prosumerName,
                houseComponentType,
                LoadtypetoSearchFor,
                _saveableEntry,
                sourceGuid,
                householdKey,
                household.HouseGuid,
                isn,
                Services.RunningConfig);
        }

        /*  [ItemNotNull]
          [NotNull]
          public List<HouseholdKeyEntry> LoadHouseholdKeyEntries()
          {
              if (Srls == null) {
                  throw new FlaException("Data Logger was null.");
              }
              return Srls.ReadFromJson<HouseholdKeyEntry>(ResultTableDefinition, Constants.GeneralHouseholdKey,
                  ExpectedResultCount.OneOrMore);
          }
          */
    }
}