using System.Collections.Generic;
using Common.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class MultiyearMultiVariableTrend {
        [NotNull]
        public Dictionary<ScenarioSliceParameters, SliceMultiValueStore> Dict { get; } = new Dictionary<ScenarioSliceParameters, SliceMultiValueStore>();

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        [NotNull]
        public SliceMultiValueStore this[[NotNull] ScenarioSliceParameters slice] {
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
            get {
                if (!Dict.ContainsKey(slice)) {
                    Dict.Add(slice, new SliceMultiValueStore(slice));
                }

                return Dict[slice];
            }
        }
    }
}