using System.Data.SQLite;
using System.IO;
using Common.Config;
using Common.Steps;
using JetBrains.Annotations;
using NPoco;

namespace Common.Database {
    public class SqlConnectionPreparer {
        [NotNull] private readonly RunningConfig _config;

        public SqlConnectionPreparer([NotNull] RunningConfig config) => _config = config;

        [NotNull]
        public MyDb GetDatabaseConnection(Stage stage, [NotNull] ScenarioSliceParameters slice, DatabaseCode databaseCode = DatabaseCode.General)
        {
            var dbfilename = GetSqlFilePath(stage, slice, databaseCode);
            var connectionString = "Data Source=" + GetSqlFilePath(stage, slice, databaseCode);
            var db = new NPoco.Database(connectionString, DatabaseType.SQLite, SQLiteFactory.Instance);
            var myDb = new MyDb(stage, slice, db, _config, connectionString, dbfilename, databaseCode);
            return myDb;
        }

        [NotNull]
        private string GetSqlFilePath(Stage stage, [NotNull] ScenarioSliceParameters slice, DatabaseCode dbCode)
        {
            var dir = FilenameHelpers.GetTargetDirectory(stage, -1, null, slice, _config);
            string dbCodeSuffix = "." + dbCode;
            return Path.Combine(dir, "Data." + stage + "." + slice.DstScenario + "." + slice.DstYear + dbCodeSuffix + ".sqlite");
        }
    }
}