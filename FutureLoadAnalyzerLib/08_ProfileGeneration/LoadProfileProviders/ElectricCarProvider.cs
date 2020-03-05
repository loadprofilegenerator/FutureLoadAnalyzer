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

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    [UsedImplicitly]
    public class ElectricCarProvider : BaseProvider, ILoadProfileProvider {
        private const string LoadtypetoSearchFor = "Car Charging Electricity";
        [NotNull] private readonly DBDto _dbDto;
        [NotNull] [ItemNotNull] private readonly List<HouseCreationAndCalculationJob> _housesToBeCreated;
        [NotNull] private readonly CachingLPGProfileLoader _lpgProfileLoader;
        [NotNull] private readonly SaveableEntry<Profile> _saveableEntry;

        public ElectricCarProvider([NotNull] ServiceRepository services,
                                   [NotNull] ScenarioSliceParameters slice,
                                   [NotNull] DBDto dbDto,
                                   [NotNull] [ItemNotNull] List<HouseCreationAndCalculationJob> housesToBeCreated,
                                   [NotNull] CachingLPGProfileLoader lpgProfileLoader) : base(nameof(ElectricCarProvider), services, slice)
        {
            _dbDto = dbDto;
            _housesToBeCreated = housesToBeCreated;
            _lpgProfileLoader = lpgProfileLoader;
            var profileCacheDb = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.EvProfiles);
            _saveableEntry = SaveableEntry<Profile>.GetSaveableEntry(profileCacheDb, SaveableEntryTableType.EvProfile, services.Logger);
            _saveableEntry.MakeTableForListOfFieldsIfNotExists(true);
        }

        [NotNull]
        public static JsonReference ChargingStationSet { get; } = new JsonReference(
            "Charging At Home with 03.7 kW, output results to Car Electricity",
            "223f0577-9249-4293-a849-ea12e2033377");

        [NotNull]
        public static JsonReference TransportationDevicesOneCar { get; } =
            new JsonReference("Bus and one slow Car", "6ac74bd0-bacd-4b39-b84a-dc7ae16702c9");

        [NotNull]
        public static JsonReference TransportationDevicesTwoCar { get; } =
            new JsonReference("Bus and two slow Cars", "f90fece2-901a-4419-8a6b-a0ed4ed6ceff");

        [NotNull]
        public static JsonReference TravelRouteSet { get; } =
            new JsonReference("Travel Route Set for 30km to Work", "0b217fce-ad99-4ef1-8540-c07081856d3c");


        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.OutboundElectricCommuter) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters)
        {
            CarDistanceEntry cde = (CarDistanceEntry)parameters.HouseComponent;
            Car myCar = _dbDto.Cars.Single(x => x.Guid == cde.CarGuid);
            parameters.HouseComponentResultObject.CarStatus = "Gasoline, not analyzed further";

            if (myCar.RequiresProfile == CarProfileRequirement.NoProfile) {
                return true;
            }

            Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == cde.HausAnschlussGuid);
            parameters.HouseComponentResultObject.CarStatus = "Electric";
            parameters.HouseComponentResultObject.CommutingDistance = cde.CommutingDistance;
            parameters.HouseComponentResultObject.OtherDrivingDistance = cde.FreizeitDistance;
            Household flaHousehold = _dbDto.Households.FirstOrDefault(x => x.Guid == cde.HouseholdGuid);
            if (flaHousehold == null) {
                parameters.HouseComponentResultObject.CarStatus += ", no household";
                throw new FlaException("no household found");
            }

            var house = _dbDto.Houses.FirstOrDefault(x => x.HouseGuid == cde.HouseGuid);
            if (house == null) {
                throw new FlaException("no house found");
            }

            HouseholdData householdData = null;
            if (_housesToBeCreated.Count > 0) {
                var houseJob = _housesToBeCreated.FirstOrDefault(x => x.House.HouseGuid == ha.HouseGuid);
                if (houseJob == null) {
                    parameters.HouseComponentResultObject.CarStatus += ", no house";

                    throw new FlaException("no house found");
                }

                householdData = houseJob.House.Households.FirstOrDefault(x => x.HouseholdGuid == flaHousehold.HouseholdKey);
                if (householdData == null) {
                    parameters.HouseComponentResultObject.CarStatus += ", no household guid";
                    throw new FlaException("no household for household guid " + cde.HouseholdGuid);
                }


                //set lpg parameters
                householdData.UseElectricCar = ElectricCarUse.UseElectricCar;

                //find number of cars
                var householdCars = _dbDto.Cars.Where(x => x.HouseholdGuid == cde.HouseholdGuid).ToList();
                switch (householdCars.Count) {
                    case 1:
                        //use lpg profile for a single car
                        householdData.TransportationDeviceSet = TransportationDevicesTwoCar;
                        break;
                    case 2:
                        //use lpg profile for a single car
                        householdData.TransportationDeviceSet = TransportationDevicesTwoCar;
                        break;
                    case 3:
                        // use lpg profile for a single car
                        // todo: fix this and put in the right transportation device set
                        householdData.TransportationDeviceSet = TransportationDevicesTwoCar;
                        break;
                    case 4:
                        //use lpg profile for a single car
                        //todo: fix this and put in the right transportation device set
                        householdData.TransportationDeviceSet = TransportationDevicesTwoCar;
                        break;
                    default: throw new FlaException("Household with " + householdCars.Count + " cars is missing");
                }

                householdData.TravelRouteSet = TravelRouteSet;
                householdData.ChargingStationSet = ChargingStationSet;
                if (householdData.TransportationDistanceModifiers == null) {
                    householdData.TransportationDistanceModifiers = new List<TransportationDistanceModifier>();
                }

                householdData.TransportationDistanceModifiers.Add(new TransportationDistanceModifier("Work", "Car", cde.CommutingDistance * 1000));
                householdData.TransportationDistanceModifiers.Add(new TransportationDistanceModifier("Entertainment",
                    "Car",
                    cde.FreizeitDistance * 1000));
                parameters.HouseComponentResultObject.CarStatus += ", asking for repare with distances commuting: " + cde.CommutingDistance +
                                                                   ", free time: " + cde.FreizeitDistance + " km";
            }

            if (Services.RunningConfig.LpgPrepareMode == LpgPrepareMode.PrepareWithFullLpgLoad) {
                Profile lpgProfile = null;
                try {
                    lpgProfile = _lpgProfileLoader.LoadLPGProfile(parameters,
                        ha.Trafokreis,
                        LoadtypetoSearchFor,
                        _saveableEntry,
                        flaHousehold.HouseholdKey,
                        out _,
                        house.ComplexName,
                        Services.RunningConfig);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Error("trying to load lpg profile: " + ex.Message);
                    parameters.HouseComponentResultObject.LPGErrors = "trying to load lpg profile failed: " + ex.Message;
                }

                // ReSharper disable PossibleNullReferenceException
                if (_housesToBeCreated.Count > 0) {
                    if (lpgProfile != null) {
                        householdData.IsCarProfileCalculated = true;
                        parameters.HouseComponentResultObject.ProcessingStatus = "Found LPG profile on full check";
                    }
                    else {
                        householdData.IsCarProfileCalculated = false;
                        parameters.HouseComponentResultObject.ProcessingStatus = "Missing LPG profile on full check";
                    }
                }
                // ReSharper restore PossibleNullReferenceException
            }
            else if (Services.RunningConfig.LpgPrepareMode == LpgPrepareMode.PrepareWithOnlyNamecheck) {
                // ReSharper disable PossibleNullReferenceException
                if (_housesToBeCreated.Count > 0) {
                    if (_saveableEntry.CheckForName(flaHousehold.HouseholdKey, Services.Logger)) {
                        householdData.IsHouseholdProfileCalculated = true;
                        parameters.HouseComponentResultObject.ProcessingStatus = "Found LPG profile on name check";
                    }
                    else {
                        householdData.IsHouseholdProfileCalculated = false;
                        parameters.HouseComponentResultObject.ProcessingStatus = "Miissing LPG profile on name check";
                    }
                }
                // ReSharper restore PossibleNullReferenceException
            }
            else {
                throw new FlaException("Unknown lpg prepare mode");
            }

            return true;
        }

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            Prosumer prosumer;
            var carDistanceEntry = (CarDistanceEntry)ppdto.HouseComponent;
            ppdto.HouseComponentResultObject.CarStatus = "Electric";
            ppdto.HouseComponentResultObject.CommutingDistance = carDistanceEntry.CommutingDistance;
            ppdto.HouseComponentResultObject.OtherDrivingDistance = carDistanceEntry.FreizeitDistance;
            var car = _dbDto.Cars.Single(x => x.Guid == carDistanceEntry.CarGuid);
            if (car.CarType != CarType.Electric) {
                ppdto.HouseComponentResultObject.CarStatus = "Not electric, ignored";
                ppdto.HouseComponentResultObject.ProfileEnergy = 0;
                return null;
            }

            if (car.RequiresProfile == CarProfileRequirement.NoProfile) {
                ppdto.HouseComponentResultObject.CarStatus = "Preexisting electric car, ignored";
                ppdto.HouseComponentResultObject.ProfileEnergy = 0;
                return null;
            }

            var ha = _dbDto.Hausanschlusse.Single(x => x.Guid == ppdto.HouseComponent.HausAnschlussGuid);
            var household = _dbDto.Households.First(x => x.Guid == carDistanceEntry.HouseholdGuid);
            if (Services.RunningConfig.CheckForLpgCalcResult) {
                try {
                    prosumer = ProvideLPGProfile(ppdto, ha, household, carDistanceEntry);
                    if (prosumer != null) {
                        ppdto.HouseComponentResultObject.ProcessingStatus = "LPG Profile";
                        ppdto.HouseComponentResultObject.CarStatus = "Electric, LPG Profile";
                        return prosumer;
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Error(ex.Message);
                }
            }

            prosumer = MakeFlatProfile(carDistanceEntry, ha, household);
            ppdto.HouseComponentResultObject.ProcessingStatus = "Flat Profile";
            ppdto.HouseComponentResultObject.CarStatus = "Electric, flat profile";
            return prosumer;
        }

        [NotNull]
        private Prosumer MakeFlatProfile([NotNull] CarDistanceEntry carDistance, [NotNull] Hausanschluss ha, [NotNull] Household household)
        {
            var slpprofile = Profile.MakeConstantProfile(carDistance.EnergyEstimate, carDistance.Name, Profile.ProfileResolution.QuarterHour);
            if (slpprofile == null) {
                throw new FlaException("No profile? Why?");
            }

            var prosumer = new Prosumer(household.HouseGuid,
                carDistance.Name,
                carDistance.HouseComponentType,
                household.SourceGuid,
                household.FinalIsn,
                household.HausAnschlussGuid,
                ha.ObjectID,
                GenerationOrLoad.Load,
                ha.Trafokreis,
                Name + " - Flat Substitute Profile",
                "Flat Substitute Profile due to missing LPG profile") {Profile = slpprofile};
            return prosumer;
        }

        [CanBeNull]
        private Prosumer ProvideLPGProfile([NotNull] ProviderParameterDto parameters,
                                           [NotNull] Hausanschluss ha,
                                           [NotNull] Household household,
                                           [NotNull] CarDistanceEntry carDistanceEntry)
        {
            string prosumerName = household.Name;
            string sourceGuid = household.SourceGuid;
            string householdKey = household.HouseholdKey;
            long isn = household.FinalIsn;
            try {
                var prosumer = _lpgProfileLoader.LoadProsumer(parameters,
                    ha,
                    prosumerName,
                    carDistanceEntry.HouseComponentType,
                    LoadtypetoSearchFor,
                    _saveableEntry,
                    sourceGuid,
                    householdKey,
                    household.HouseGuid,
                    isn,
                    Services.RunningConfig);
                if (prosumer == null) {
                    return null;
                }

                if (prosumer.Profile == null) {
                    throw new FlaException("Profile was null");
                }

                parameters.HouseComponentResultObject.ActualDrivingDistance = prosumer.Profile.EnergySum() / 15.0 * 100;
                return prosumer;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                Error(ex.Message);
                parameters.HouseComponentResultObject.ErrorMessage = ex.Message;
            }

            return null;
        }
    }
}