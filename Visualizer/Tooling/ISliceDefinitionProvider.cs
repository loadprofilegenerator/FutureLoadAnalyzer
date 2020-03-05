using System.Collections.Generic;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling {
    public interface ISliceDefinitionProvider {
        [ItemNotNull]
        [NotNull]
        List<ScenarioSliceParameters> MakeSlices();
    }
}