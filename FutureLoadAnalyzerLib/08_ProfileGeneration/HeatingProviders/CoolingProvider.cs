using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class CoolingProvider : BaseProvider, ILoadProfileProvider {
        [NotNull]
        private readonly Dictionary<AirConditioningType, CoolingProfileGenerator> _coolingProfileGeneratorsByType =
            new Dictionary<AirConditioningType, CoolingProfileGenerator>();

        [NotNull]
        private readonly DBDto _dbDto;

        public CoolingProvider([NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice, [NotNull] DBDto dbDto)
            : base(nameof(CoolingProvider), services, slice)
        {
            _dbDto = dbDto;

            // TODO: init properly, read profiles
            var dbRaw = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var temperatures = dbRaw.Fetch<TemperatureProfileImport>();
            var temp = temperatures.Single(x => x.Jahr == slice.DstYear);
            Profile temperaturProfileHourly = new Profile(temp.Profile ?? throw new FlaException("missing profile"));
            _coolingProfileGeneratorsByType.Add(AirConditioningType.Residential,
                new CoolingProfileGenerator(temperaturProfileHourly, 23, 21, MyLogger));
            _coolingProfileGeneratorsByType.Add(AirConditioningType.Commercial,
                new CoolingProfileGenerator(temperaturProfileHourly, 18, 17, MyLogger));
            _coolingProfileGeneratorsByType.Add(AirConditioningType.Industrial,
                new CoolingProfileGenerator(temperaturProfileHourly, 12, 10, MyLogger));
        }

        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Cooling) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            AirConditioningEntry hse = (AirConditioningEntry)ppdto.HouseComponent;
            if (hse.HouseComponentType != HouseComponentType.Cooling) {
                throw new FlaException("Wrong type");
            }

            Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == hse.HausAnschlussGuid);
            var pa = new Prosumer(hse.HouseGuid,
                hse.Standort,
                hse.HouseComponentType,
                hse.SourceGuid,
                hse.FinalIsn,
                hse.HausAnschlussGuid,
                ha.ObjectID,
                GenerationOrLoad.Load,
                ha.Trafokreis,
                Name,
                "Cooling State Engine");

            // todo: randomize this with buckets and/or simulate a central control
            double targetRuntimePerDay = 2 + Services.Rnd.NextDouble() * 2;
            double trigger = 1 - Services.Rnd.NextDouble() * 0.1;
            CoolingCalculationParameters hpc = new CoolingCalculationParameters(targetRuntimePerDay, trigger);
            var hpr = _coolingProfileGeneratorsByType[hse.AirConditioningType].Run(hpc, hse.EffectiveEnergyDemand, Services.Rnd);
            ppdto.HouseComponentResultObject.CoolingType = hse.AirConditioningType.ToString();
            pa.Profile = hpr.GetEnergyDemandProfile().ScaleToTargetSum(hse.EffectiveEnergyDemand, "Air Conditioning Profile", out var _);
            if (Math.Abs(pa.Profile.EnergySum() - hse.EffectiveEnergyDemand) > 1) {
                throw new FlaException("Energy sum from the cooling is all wrong. Should be " + hse.EffectiveEnergyDemand + " but was " +
                                       pa.Profile.EnergySum());
            }

            return pa;
        }
    }
}