using System;
using System.Collections.Generic;
using Common.Config;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Database {
    public class SingleSliceResultRepository {
        [NotNull] private readonly Dictionary<Type, ISingleTypeRepository> _collections = new Dictionary<Type, ISingleTypeRepository>();
        [NotNull] private readonly RunningConfig _rc;
        [NotNull] private readonly ScenarioSliceParameters _slice;

        public SingleSliceResultRepository([NotNull] ScenarioSliceParameters slice, [NotNull] RunningConfig rc)
        {
            _slice = slice;
            _rc = rc;
        }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<T> Fetch<T>() where T : class, IGuidProvider
        {
            var t = typeof(T);
            if (!_collections.ContainsKey(t)) {
                var newrep = new SingleTypeCollection<T>(_rc, _slice);
                _collections.Add(t, newrep);
            }

            SingleTypeCollection<T> rep = (SingleTypeCollection<T>)_collections[t];
            return rep;
        }
    }
}