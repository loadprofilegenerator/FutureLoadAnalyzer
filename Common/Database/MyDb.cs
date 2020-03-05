using System;
using System.Collections.Generic;
using System.Threading;
using Automation;
using Common.Config;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using SQLite;
using IgnoreAttribute = NPoco.IgnoreAttribute;

namespace Common.Database {
    public class MyDb {
        [ItemNotNull] [JetBrains.Annotations.NotNull]
        private static readonly HashSet<string> _checkedTypes = new HashSet<string>();

        [JetBrains.Annotations.NotNull] private static readonly Dictionary<string, string> _databasesInTransaction = new Dictionary<string, string>();

        [JetBrains.Annotations.NotNull] private static readonly Dictionary<string, bool> _isInTransaction = new Dictionary<string, bool>();

        [JetBrains.Annotations.NotNull] private static readonly Dictionary<string, string> _transactionSetters = new Dictionary<string, string>();
        [JetBrains.Annotations.NotNull] public static readonly object Dblock = new object();
        [JetBrains.Annotations.NotNull] private readonly RunningConfig _config;
        [JetBrains.Annotations.NotNull] private readonly string _connectionString;

        [JetBrains.Annotations.NotNull] private readonly NPoco.Database _database;

        // ReSharper disable once NotAccessedField.Local
        private readonly DatabaseCode _dbCode;
        [JetBrains.Annotations.NotNull] private readonly ScenarioSliceParameters _slice;

        public MyDb(Stage stage,
                    [JetBrains.Annotations.NotNull] ScenarioSliceParameters slice,
                    [JetBrains.Annotations.NotNull] NPoco.Database database,
                    [JetBrains.Annotations.NotNull] RunningConfig config,
                    [JetBrains.Annotations.NotNull] string connectionString,
                    [JetBrains.Annotations.NotNull] string dbFilename,
                    DatabaseCode dbCode)
        {
            _slice = slice;
            _config = config;
            _connectionString = connectionString;
            _dbCode = dbCode;
            MyStage = stage;
            _database = database;
            DBFilename = dbFilename;
        }

        [JetBrains.Annotations.NotNull]
        // ReSharper disable once InconsistentlySynchronizedField
        public string ConnectionString => _database.ConnectionString;

        [JetBrains.Annotations.NotNull]
        public string DBFilename { get; }

        public Stage MyStage { get; set; }

        public void BeginTransaction()
        {
            lock (Dblock) {
                if (_databasesInTransaction.ContainsKey(_database.ConnectionString)) {
                    throw new FlaException("double entry for " + _database.ConnectionString);
                }

                _databasesInTransaction.Add(_database.ConnectionString, AutomationUtili.GetCallingMethodAndClass());
                _database.BeginTransaction();
                _isInTransaction[DBFilename] = true;
                _transactionSetters[DBFilename] = AutomationUtili.GetCallingMethodAndClass();
            }
        }

        public void CloseSharedConnection()
        {
            lock (Dblock) {
                _database.CloseSharedConnection();
            }
        }

        public void CompleteTransaction()
        {
            lock (Dblock) {
                _databasesInTransaction.Remove(_database.ConnectionString);
                _database.CompleteTransaction();
                _isInTransaction[DBFilename] = false;
            }
        }

        public void CreateTableIfNotExists<T>()
        {
            lock (Dblock) {
                using (var dbcon = new SQLiteConnection(DBFilename)) {
                    dbcon.CreateTable<T>();
                }
            }
        }

        public int Delete<T>([JetBrains.Annotations.NotNull] T obj)
        {
            lock (Dblock) {
                return _database.Delete<T>(obj);
            }
        }

        public int Execute([JetBrains.Annotations.NotNull] string sql)
        {
            lock (Dblock) {
                return _database.Execute(sql);
            }
        }

        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<T> Fetch<T>()
        {
            int count = 0;
            while (IsThisInTransaction()) {
                string fn = _transactionSetters[DBFilename];
                SLogger.Info("Waiting for transaction for 0.5s, caller was " + fn);
                Thread.Sleep(500);
                count++;
                if (count > 240 * 2) {
                    throw new FlaException("Waited more than 240s");
                }
            }

            SLogger.Debug("Fetching all " + typeof(T).FullName + " from " + DBFilename);
            lock (Dblock) {
                return _database.Fetch<T>() ?? throw new FlaException("Fetch failed");
            }
        }

        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<T> Fetch<T>([JetBrains.Annotations.NotNull] string sql)
        {
            lock (Dblock) {
                return _database.Fetch<T>(sql) ?? throw new FlaException("Fetch failed");
            }
        }

        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public SingleTypeCollection<T> FetchAsRepo<T>() where T : class, IGuidProvider
        {
            SLogger.Debug("Fetching all " + typeof(T).FullName + " from " + DBFilename);
            SingleTypeCollection<T> repo = new SingleTypeCollection<T>(_config, _slice);
            return repo;
        }

        [JetBrains.Annotations.NotNull]
        public string GetConnectionstring() => _connectionString;

        [JetBrains.Annotations.NotNull]
        public string GetResultFullPath(int sequenceNumber, [JetBrains.Annotations.NotNull] string className) =>
            FilenameHelpers.GetTargetDirectory(MyStage, sequenceNumber, className, _slice, _config);

        public void Insert<T>([JetBrains.Annotations.NotNull] T obj)
        {
            lock (Dblock) {
                _database.Insert(obj);
            }
        }

        public void RecreateTable<T>()
        {
            lock (Dblock) {
                using (var dbcon = new SQLiteConnection(DBFilename)) {
                    dbcon.DropTable<T>();
                    dbcon.CreateTable<T>();
                }
            }
        }

        public void Save<T>([JetBrains.Annotations.NotNull] T obj)
        {
            if (!IsThisInTransaction()) {
                throw new FlaException("Saving without transaction");
            }

            Type t = obj.GetType();
            if (!_checkedTypes.Contains(t.FullName)) {
                var propinfos = t.GetProperties();
                foreach (var propertyInfo in propinfos) {
                    if (!propertyInfo.CanWrite) {
                        var hasIgnore = Attribute.IsDefined(propertyInfo, typeof(IgnoreAttribute));
                        if (!hasIgnore) {
                            throw new FlaException("Readonly property found on " + t.FullName + ": " + propertyInfo.Name);
                        }
                    }
                }

                _checkedTypes.Add(t.FullName);
            }

            lock (Dblock) {
                _database.Save(obj);
            }
        }

        private bool IsThisInTransaction()
        {
            if (!_isInTransaction.ContainsKey(DBFilename)) {
                _isInTransaction.Add(DBFilename, false);
            }

            return _isInTransaction[DBFilename];
        }
    }
}