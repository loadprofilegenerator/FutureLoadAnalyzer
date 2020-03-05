using System.Collections.Generic;
using System.Linq;
using Common;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    public class ProviderCollection {
        public ProviderCollection([NotNull] [ItemNotNull] ILoadProfileProvider[] providers)
        {
            Providers = providers.ToList();
        }

        [NotNull]
        [ItemNotNull]
        public List<ILoadProfileProvider> Providers { get; }
        [NotNull]
        public ILoadProfileProvider GetCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            var providers = Providers.Where(x => x.IsCorrectProvider(houseComponent)).ToList();
            if (providers.Count != 1) {
                throw new FlaException("Not exactly one provider found for " + houseComponent.HouseComponentType + " : " + string.Join(",", providers.Select(x=> x.Name)));
            }
            return providers[0];
        }
    }
}