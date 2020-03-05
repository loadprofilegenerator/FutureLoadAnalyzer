using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class BusinessProfileProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] [ItemNotNull]
        private static readonly List<BusinessProfileOverrideEntry> _usedProfileOverrides = new List<BusinessProfileOverrideEntry>();

        [NotNull] private readonly DBDto _dbDto;
        [NotNull] private readonly BusinessProfileOverrideRepository _repository;
        [NotNull] private readonly SLPProvider _slpProvider;

        public BusinessProfileProvider([NotNull] ServiceRepository services,
                                       [NotNull] ScenarioSliceParameters slice,
                                       [NotNull] SLPProvider slpProvider,
                                       [NotNull] DBDto dbDto) : base(nameof(BusinessProfileProvider), services, slice)
        {
            _slpProvider = slpProvider;
            _dbDto = dbDto;
            DevelopmentStatus.Add("Make visualisations of the sum profile of each household");
            DevelopmentStatus.Add("add the original isn of each business to the business entry");
            DevelopmentStatus.Add("use the correct business isn");
            DevelopmentStatus.Add("add the total energy checks back in");
            DevelopmentStatus.Add("connect the business to the correct hausanschluss");
            _repository = new BusinessProfileOverrideRepository(services.RunningConfig);
        }

        public override void DoFinishCheck()
        {
            _repository.CheckIfAllAreUsed(_usedProfileOverrides);
        }


        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.BusinessNoLastgangLowVoltage) {
                return true;
            }

            if (houseComponent.HouseComponentType == HouseComponentType.BusinessNoLastgangHighVoltage) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            BusinessEntry be = (BusinessEntry)ppdto.HouseComponent;
            if (be.BusinessType == BusinessType.Unknown) {
                throw new FlaException("unknown business type");
            }

            ppdto.HouseComponentResultObject.BusinessCategory = be.BusinessType.ToString();
            //string profileToUse = GetCorrectProfile(be.BusinessType);
            if (be.HouseComponentType == HouseComponentType.BusinessNoLastgangLowVoltage) {
                Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == be.HausAnschlussGuid);
                var profile = GetCorrectProfile(be.BusinessType, be.EffectiveEnergyDemand, be);
                var pa = new Prosumer(be.HouseGuid,
                    be.Standort,
                    be.HouseComponentType,
                    be.Guid,
                    be.FinalIsn,
                    be.HausAnschlussGuid,
                    ha.ObjectID,
                    GenerationOrLoad.Load,
                    ha.Trafokreis,
                    Name,
                    profile.Name);
                pa.Profile = profile;
                pa.SumElectricityPlanned = be.EffectiveEnergyDemand;
                return pa;
            }

            if (be.HouseComponentType == HouseComponentType.BusinessNoLastgangHighVoltage) {
                Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == be.HausAnschlussGuid);
                var profile = GetCorrectProfile(be.BusinessType, be.EffectiveEnergyDemand, be);
                var pa = new Prosumer(be.HouseGuid,
                    be.Standort,
                    be.HouseComponentType,
                    be.Guid,
                    be.FinalIsn,
                    be.HausAnschlussGuid,
                    ha.ObjectID,
                    GenerationOrLoad.Load,
                    ha.Trafokreis,
                    Name,
                    profile.Name);
                pa.Profile = GetCorrectProfile(be.BusinessType, be.EffectiveEnergyDemand, be);
                pa.SumElectricityPlanned = be.EffectiveEnergyDemand;
                ppdto.HouseComponentResultObject.ProcessingStatus = "High Voltage Supplied Business without RLM";
                return pa;
            }

            throw new FlaException("No profile could be created");
        }

        [NotNull]
        private Profile GetCorrectProfile(BusinessType beBusinessType, double targetEnergy, [NotNull] BusinessEntry be)
        {
            var overrideEntry = _repository.GetEntry(be.ComplexName, be.BusinessName, be.Standort);
            if (overrideEntry != null) {
                _usedProfileOverrides.Add(overrideEntry);
                if (overrideEntry.ProfileName.ToLower() == "flat") {
                    return Profile.MakeConstantProfile(targetEnergy, "Flat", Profile.ProfileResolution.QuarterHour);
                }

                return _slpProvider.Run(overrideEntry.ProfileName, targetEnergy);
            }

            switch (beBusinessType) {
                case BusinessType.Büro:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Shop:
                    return _slpProvider.Run("G4", targetEnergy);
                case BusinessType.Werkstatt:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Seniorenheim:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Restaurant:
                    return _slpProvider.Run("G2", targetEnergy);
                case BusinessType.Bäckerei:
                    return _slpProvider.Run("G5", targetEnergy);
                case BusinessType.Industrie:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Sonstiges:
                    return _slpProvider.Run("G0", targetEnergy);
                case BusinessType.Praxis:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Kirche:
                    return _slpProvider.Run("G6", targetEnergy);
                case BusinessType.Schule:
                    return _slpProvider.Run("G1", targetEnergy);
                case BusinessType.Tankstelle:
                    return _slpProvider.Run("G4", targetEnergy);
                case BusinessType.Wasserversorgung:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Brauerei:
                    return Profile.MakeConstantProfile(targetEnergy, "Flat", Profile.ProfileResolution.QuarterHour);
                case BusinessType.Hotel:
                    return _slpProvider.Run("G0", targetEnergy);
                case BusinessType.Museum:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Hallenbad:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Eissport:
                    return _slpProvider.Run("G3", targetEnergy);
                case BusinessType.Mobilfunk:
                    return Profile.MakeConstantProfile(targetEnergy, "Flat", Profile.ProfileResolution.QuarterHour);
                case BusinessType.Unknown:
                    throw new ArgumentOutOfRangeException(nameof(beBusinessType), beBusinessType, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(beBusinessType), beBusinessType, null);
            }
        }
    }
}