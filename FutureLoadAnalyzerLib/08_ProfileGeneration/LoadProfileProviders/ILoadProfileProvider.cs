using System;
using System.Collections.Generic;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public interface ILoadProfileProvider {

        void DoFinishCheck();
        [NotNull]
        [ItemNotNull]
        List<string> DevelopmentStatus { get; }

        [NotNull]
        string Name { get; }

        bool IsCorrectProvider([NotNull] IHouseComponent houseComponent);
        bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters);

        [CanBeNull]
        Prosumer ProvideProfile([NotNull] ProviderParameterDto parameters);

        TimeSpan Elapsed { get; }
    }
}