using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Config;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Database {
    public class SingleTypeCollection<T> : ISingleTypeRepository, IEnumerable<T> where T : class, IGuidProvider {
        [NotNull] private readonly RunningConfig _rc;
        [NotNull] private readonly ScenarioSliceParameters _slice;

        [CanBeNull] [ItemNotNull] private List<T> _values;

        [CanBeNull] private Dictionary<string, T> _valuesByGuid;

        [NotNull] private Dictionary<string, Dictionary<string, List<T>>> _valuesByReferenceGuid =
            new Dictionary<string, Dictionary<string, List<T>>>();

        public SingleTypeCollection([NotNull] RunningConfig rc, [NotNull] ScenarioSliceParameters slice)
        {
            _rc = rc;
            _slice = slice;
        }

        public int Count {
            get {
                var l = GetValueList();
                return l.Count;
            }
        }

        [NotNull]
        public T this[int index] {
            get => GetValueList()[index];
            set {
                GetValueList().Insert(index, value);
                ClearCache();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() => GetValueList().GetEnumerator();

        public void Add([NotNull] T cde)
        {
            GetValueList().Add(cde);
            ClearCache();
        }

        [NotNull]
        public T GetByGuid([NotNull] string guid)
        {
            if (_valuesByGuid == null) {
                var list = GetValueList();
                _valuesByGuid = list.ToDictionary(x => x.Guid, x => x);
            }

            return _valuesByGuid[guid];
        }

        [NotNull]
        [ItemNotNull]
        public List<T> GetByReferenceGuid([NotNull] string referenceGuid, [NotNull] string name, [NotNull] Func<T, string> referenceGuidFunc)
        {
            if (!_valuesByReferenceGuid.ContainsKey(name)) {
                InitializeDictionary(name, referenceGuidFunc);
            }

            // ReSharper disable once PossibleNullReferenceException
            return _valuesByReferenceGuid[name][referenceGuid];
        }

        [NotNull]
        [ItemNotNull]
        public List<T> GetByReferenceGuidWithEmptyReturns([NotNull] string referenceGuid,
                                                          [NotNull] string name,
                                                          [NotNull] Func<T, string> referenceGuidFunc)
        {
            if (!_valuesByReferenceGuid.ContainsKey(name)) {
                InitializeDictionary(name, referenceGuidFunc);
            }

            // ReSharper disable once PossibleNullReferenceException
            if (!_valuesByReferenceGuid[name].ContainsKey(referenceGuid)) {
                return new List<T>();
            }

            return _valuesByReferenceGuid[name][referenceGuid];
        }

        [NotNull]
        [ItemNotNull]
        public List<T> GetValueList()
        {
            if (_values == null) {
                SqlConnectionPreparer preparer;
                preparer = new SqlConnectionPreparer(_rc);
                var db = preparer.GetDatabaseConnection(Stage.Houses, _slice);
                _values = db.Fetch<T>();
            }

            return _values;
        }

        public void Remove([NotNull] T itemToRemove)
        {
            GetValueList().Remove(itemToRemove);
            ClearCache();
        }

        public void SaveAll([NotNull] MyDb db, bool clearIds, bool useTransaction)
        {
            if (useTransaction) {
                db.BeginTransaction();
            }

            foreach (var obj in GetValueList()) {
                if (clearIds) {
                    obj.ID = 0;
                }

                db.Save(obj);
            }

            if (useTransaction) {
                db.CompleteTransaction();
            }
        }

        [NotNull]
        [ItemNotNull]
        public HashSet<string> ToGuidHashset()
        {
            var l = GetValueList();
            var hashSet = new HashSet<string>();
            foreach (var str in l) {
                hashSet.Add(str.Guid);
            }

            return hashSet;
        }

        [NotNull]
        [ItemNotNull]
        public HashSet<string> ToReferenceGuidHashset([NotNull] Func<T, string> refGuidFunc)
        {
            var l = GetValueList();
            var hashSet = new HashSet<string>();
            foreach (var str in l) {
                hashSet.Add(refGuidFunc(str));
            }

            return hashSet;
        }

        private void ClearCache()
        {
            _valuesByGuid = null;
            _valuesByReferenceGuid = new Dictionary<string, Dictionary<string, List<T>>>();
        }

        private void InitializeDictionary([NotNull] string name, [NotNull] Func<T, string> referenceGuidFunc)
        {
            if (_valuesByReferenceGuid.ContainsKey(name)) {
                throw new FlaException("dict not null");
            }

            if (referenceGuidFunc == null) {
                throw new FlaException("No reference Guid Function was set");
            }

            var list = GetValueList();
            _valuesByReferenceGuid[name] = new Dictionary<string, List<T>>();
            var d = _valuesByReferenceGuid[name];
            foreach (var c in list) {
                var refGuid = referenceGuidFunc(c);
                if (!d.ContainsKey(refGuid)) {
                    d.Add(refGuid, new List<T>());
                }

                d[refGuid].Add(c);
            }
        }
    }
}