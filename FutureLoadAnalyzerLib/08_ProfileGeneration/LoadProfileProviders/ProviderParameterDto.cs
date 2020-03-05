using Data.DataModel.Creation;
using FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class ProviderParameterDto {
        public ProviderParameterDto([NotNull] IHouseComponent houseComponent,
                                    [NotNull] string lpgDirectoryInfo,
                                    [NotNull] HouseComponentRo houseComponentResultObject)
        {
            HouseComponent = houseComponent;
            LPGDirectoryInfo = lpgDirectoryInfo;
            HouseComponentResultObject = houseComponentResultObject;
        }


        [NotNull]
        public IHouseComponent HouseComponent { get; }

        [NotNull]
        public HouseComponentRo HouseComponentResultObject { get; }

        [NotNull]
        public string LPGDirectoryInfo { get; }
    }
}