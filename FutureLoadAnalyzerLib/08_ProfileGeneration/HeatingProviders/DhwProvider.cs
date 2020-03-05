using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class DhwProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] private readonly DBDto _dbDto;
        [NotNull] private readonly DhwProfileGenerator _dhw;

        public DhwProvider([NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice, [NotNull] DBDto dbDto) : base(
            nameof(DhwProvider),
            services,
            slice)
        {
            _dbDto = dbDto;
            _dhw = new DhwProfileGenerator();
        }


        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Dhw) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            DHWHeaterEntry dhw = (DHWHeaterEntry)ppdto.HouseComponent;
            if (dhw.HouseComponentType != HouseComponentType.Dhw) {
                throw new FlaException("Wrong type");
            }

            ppdto.HouseComponentResultObject.DhwSystemType = dhw.DhwHeatingSystemType.ToString();
            if (dhw.DhwHeatingSystemType != DhwHeatingSystem.Electricity && dhw.DhwHeatingSystemType != DhwHeatingSystem.Heatpump) {
                ppdto.HouseComponentResultObject.HeatingSystemMessage = "Not electric heating";
                return null;
            }

            Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == dhw.HausAnschlussGuid);
            var pa = new Prosumer(dhw.HouseGuid,
                dhw.Name,
                dhw.HouseComponentType,
                dhw.SourceGuid,
                dhw.FinalIsn,
                dhw.HausAnschlussGuid,
                ha.ObjectID,
                GenerationOrLoad.Load,
                ha.Trafokreis,
                Name,
                "DHW Profile Generator");
            //todo: randomize this with buckets and/or simulate a central control
            int startTime = 2 * 4 + Services.Rnd.Next(12);
            int stopTime = startTime + 3 * 4 + Services.Rnd.Next(12);
            //double targetRuntimePerDay = 3*4 + Services.Rnd.NextDouble() * 4;
            double trigger = 1 - 0.05 * Services.Rnd.NextDouble();
            DhwCalculationParameters dhwCalculationParameters = new DhwCalculationParameters(startTime, stopTime, trigger);
            var dhwResult = _dhw.Run(dhwCalculationParameters, dhw.EffectiveEnergyDemand, Services.Rnd);
            pa.Profile = dhwResult.GetEnergyDemandProfile();
            return pa;
        }
    }
}