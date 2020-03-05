using System.Collections.Generic;
using Common.Config;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Database {
    public class AnalysisRepository {
        [NotNull] private readonly object _mylock = new object();
        [NotNull] private readonly RunningConfig _rc;

        [NotNull] private readonly Dictionary<ScenarioSliceParameters, SingleSliceResultRepository> _sliceRepos =
            new Dictionary<ScenarioSliceParameters, SingleSliceResultRepository>();

        public AnalysisRepository([NotNull] RunningConfig rc) => _rc = rc;

        [NotNull]
        public SingleSliceResultRepository GetSlice([NotNull] ScenarioSliceParameters slice)
        {
            lock (_mylock) {
                if (!_sliceRepos.ContainsKey(slice)) {
                    _sliceRepos.Add(slice, new SingleSliceResultRepository(slice, _rc));
                }

                return _sliceRepos[slice];
            }
        }
    }
}