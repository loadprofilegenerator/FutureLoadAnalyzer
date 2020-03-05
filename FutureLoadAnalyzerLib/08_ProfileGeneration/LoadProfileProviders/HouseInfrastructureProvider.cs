using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class HouseInfrastructureProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] private readonly DBDto _dbDto;

        public HouseInfrastructureProvider([NotNull] ServiceRepository services,
                                           [NotNull] ScenarioSliceParameters slice,
                                           [NotNull] DBDto dbDto) : base(nameof(HouseInfrastructureProvider),
            services,
            slice)
        {
            _dbDto = dbDto;
        }


        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Infrastructure) {
                return true;
            }
            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            BuildingInfrastructure be = (BuildingInfrastructure)ppdto.HouseComponent;
            if (be.HouseComponentType == HouseComponentType.Infrastructure) {
                Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == be.HausAnschlussGuid);
                var pa = new Prosumer(be.HouseGuid,
                    be.Standort ?? "nameless house infrastructure",
                    be.HouseComponentType,
                    be.SourceGuid,
                    be.FinalIsn,
                    be.HausAnschlussGuid,
                    ha.ObjectID,
                    GenerationOrLoad.Load,
                    ha.Trafokreis,Name,"Flat House Infrastructure");
                pa.Profile = Profile.MakeConstantProfile(be.EffectiveEnergyDemand, "Flat House Infrastucture", Profile.ProfileResolution.QuarterHour);
                if (pa.Profile.EnergyOrPower == EnergyOrPower.Power) {
                    pa.Profile = pa.Profile.ConvertFromPowerToEnergy();
                }

                pa.SumElectricityPlanned = be.EffectiveEnergyDemand;
                return pa;
            }
            throw new FlaException("No profile could be created");
        }
    }
}