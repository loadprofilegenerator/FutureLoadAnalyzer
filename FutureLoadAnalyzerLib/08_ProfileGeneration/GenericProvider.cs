/*using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    /// <summary>
    /// this is for testing only
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class GenericProvider : ILoadProfileProvider {
        [NotNull] private DBDto _dbDto;

        public GenericProvider([NotNull] DBDto dbDto)
        {
            _dbDto = dbDto;
        }

        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Household) {
                return false;
            }
            if (houseComponent.HouseComponentType == HouseComponentType.BusinessNoLastgangLowVoltage) {
                return false;
            }
            if (houseComponent.HouseComponentType == HouseComponentType.Photovoltaik) {
                return false;
            }
            if (houseComponent.HouseComponentType == HouseComponentType.BusinessWithLastgangLowVoltage) {
                return false;
            }
            if (houseComponent.HouseComponentType == HouseComponentType.BusinessWithLastgangHighVoltage) {
                return false;
            }
            if (houseComponent.HouseComponentType == HouseComponentType.BusinessNoLastgangHighVoltage) {
                return false;
            }
            if (houseComponent.HouseComponentType == HouseComponentType.OutboundElectricCommuter) {
                return false;
            }
            
            return true;
        }

        [NotNull]
        public Prosumer ProvideProfile([NotNull] ProviderParameterDto parameters)
        {
            //var ha = _hausanschlusses.Single(x => x.HausanschlussGuid == houseComponent.HausAnschlussGuid);
            var ha = _dbDto.Hausanschlusse.Single(x => x.HausanschlussGuid == parameters.HouseComponent.HausAnschlussGuid);
            return new Prosumer( parameters.HouseComponent.HouseGuid, parameters.HouseComponent.Name,
                parameters.HouseComponent.HouseComponentType, parameters.HouseComponent.SourceGuid,
                parameters.HouseComponent.FinalIsn,
                parameters.HouseComponent.HausAnschlussGuid,"",GenerationOrLoad.Load,
                ha.Trafokreis, Name);
        }

        public TimeSpan Elapsed => TimeSpan.Zero;


        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters)
        {
            return true;
        }

        [NotNull]
        List<string> ILoadProfileProvider.DevelopmentStatus { get; } = new List<string>();
        [NotNull]
        public string Name { get; } = "GenericProvider";
    }
}*/