using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Common;
using Common.Config;
using Common.Database;
using Common.Logging;
using Common.Steps;
using Data.DataModel.Export;
using JetBrains.Annotations;
using MessagePack;

namespace Data.Database {
    public class SaveableEntry<T> : BasicLoggable where T : BasicSaveable<T>, new() {
        [CanBeNull] [ItemNotNull] private HashSet<string> _namesInDatabase;

        private SaveableEntry([NotNull] MyDb sqliteDB, [NotNull] string tableName, [NotNull] ILogger logger) : base(logger,
            sqliteDB.MyStage,
            "SaveableEntry")
        {
            SqliteDB = sqliteDB;
            TableName = tableName;
        }

        [NotNull]
        [ItemNotNull]
        public List<FieldDefinition> Fields { get; } = new List<FieldDefinition>();

        [NotNull]
        [ItemNotNull]
        public List<List<RowValue>> RowEntries { get; } = new List<List<RowValue>>();

        [NotNull]
        public MyDb SqliteDB { get; }

        [NotNull]
        public string TableName { get; }

        public void AddField([NotNull] string name, SqliteDataType datatype)
        {
            Fields.Add(new FieldDefinition(name, datatype));
        }

        public void AddField([NotNull] string name, [NotNull] Type datatype)
        {
            string sqlDataType;
            switch (datatype.Name) {
                case "String":
                    sqlDataType = "TEXT";
                    break;
                case "Int32":
                    sqlDataType = "INTEGER";
                    break;
                case "Boolean":
                    sqlDataType = "BIT";
                    break;
                case "DateTime":
                    sqlDataType = "DateTime";
                    break;
                default:
                    throw new Exception("Unknown data type:" + datatype.Name);
            }

            Fields.Add(new FieldDefinition(name, sqlDataType));
        }

        public void AddRow([NotNull] T obj)
        {
            RowEntries.Add(obj.GetRowForDatabase());
        }

        public bool CheckForName([NotNull] string myKey, [NotNull] ILogger logger)
        {
            if (_namesInDatabase == null) {
                RefreshNamesInDatabase(logger);
            }

            // ReSharper disable once PossibleNullReferenceException
            if (_namesInDatabase.Contains(myKey)) {
                return true;
            }

            return false;
        }

        public void ClearTable()
        {
            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                var command1 = conn.CreateCommand();
                command1.CommandText = "DELETE FROM " + TableName;
                var result1 = command1.ExecuteNonQuery();
                if (result1 != 0 && result1 != -1) {
                    throw new Exception("Dropping the table " + TableName + " failed: Result: " + result1);
                }

                Info("Deleted " + result1 + " entries from " + TableName);
            }
        }

        public void CreateIndexIfNotExists([NotNull] string tgtField)
        {
            var indexCmd = "CREATE INDEX if not EXISTS " + TableName + "_idx ON " + TableName + "(" + tgtField + ");";

            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = indexCmd;
                command.ExecuteNonQuery();
            }
        }

        public void DeleteEntryByName([NotNull] string myKey)
        {
            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                var command1 = conn.CreateCommand();
                command1.CommandText = "DELETE FROM " + TableName + " WHERE Name = @par1";
                Info("Deleting from " + TableName + " the name " + myKey);
                command1.Parameters.AddWithValue("@par1", myKey);
                command1.ExecuteScalar();
            }
        }

        [NotNull]
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static SaveableEntry<T> GetSaveableEntry([NotNull] MyDb db, SaveableEntryTableType saveableEntryTableType, [NotNull] ILogger logger)
#pragma warning restore CA1000 // Do not declare static members on generic types
        {
            logger.Debug("Using db for " + typeof(T).Name + " logging at " + db.GetConnectionstring() + " with the table " + saveableEntryTableType,
                db.MyStage,
                "Profile");
            var sa = new SaveableEntry<T>(db, typeof(T).Name + "_" + saveableEntryTableType, logger);
            var t = new T();
            t.SetFieldListToSave(sa.AddField);
            return sa;
        }

        public void IntegrityCheck()
        {
            if (Fields.Count != RowEntries[0].Count) {
                throw new Exception("Inconsistent number of columns");
            }
        }

        [NotNull]
        [ItemNotNull]
        public List<T> LoadAllOrMatching([CanBeNull] string fieldName = null, [CanBeNull] string key = null)
        {
            var query = "select MessagePack from " + TableName;
            if (key != null && fieldName != null) {
                query += " WHERE " + fieldName + " = @key";
            }

            List<T> entries = new List<T>();
            //Info("Reading from " + SqliteDB.DBFilename);
            using (var con = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                using (var cmd = new SQLiteCommand(con)) {
                    cmd.CommandText = query;
                    if (key != null && fieldName != null) {
                        cmd.Parameters.AddWithValue("@key", key);
                    }

                    con.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        var p = ReadSingleLine(reader);
                        entries.Add(p);
                    }

                    reader.Close();
                    con.Close();
                }
            }

            return entries;
        }

        [NotNull]
        public T LoadExactlyOneByName([NotNull] string key)
        {
            var query = "select * from " + TableName;
            query += " WHERE Name like '" + key + "'";

            List<T> entries;
            using (var con = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                using (var cmd = new SQLiteCommand(con)) {
                    cmd.CommandText = query;
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    entries = new List<T>();
                    while (reader.Read()) {
                        // ReSharper disable once CoVariantArrayConversion
                        var loadedEntry = ReadSingleLine(reader);
                        entries.Add(loadedEntry);
                    }

                    reader.Close();
                    con.Close();
                }
            }

            if (entries.Count > 1) {
                throw new FlaException("Not exactly 1 profile in database found for " + key);
            }

            if (entries.Count < 1) {
                throw new FlaException("no profile in database found for " + key);
            }

            return entries[0];
        }

        public void MakeCleanTableForListOfFields(bool makeIndexOnName)
        {
            if (Fields.Count == 0) {
                throw new Exception("No fields defined for database");
            }

            var sqlDel = "DROP TABLE IF EXISTS " + TableName;

            var sql = "CREATE TABLE " + TableName + "(";
            foreach (var field in Fields) {
                sql += field.Name + " " + field.Type + ",";
            }

            var indexCmd = "CREATE INDEX if not EXISTS " + TableName + "_idx ON " + TableName + "(Name);";
            sql = sql.Substring(0, sql.Length - 1) + ");";
            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                var command1 = conn.CreateCommand();
                command1.CommandText = sqlDel;
                var result1 = command1.ExecuteNonQuery();
                if (result1 != 0 && result1 != -1) {
                    throw new Exception("Dropping the table " + TableName + " failed: Result: " + result1);
                }

                var command = conn.CreateCommand();
                command.CommandText = sql;
                var result = command.ExecuteNonQuery();
                if (result != 0) {
                    throw new Exception("Creating the table " + TableName + " failed.");
                }

                if (makeIndexOnName) {
                    var commandIdx = conn.CreateCommand();
                    commandIdx.CommandText = indexCmd;
                    var resultIdx = commandIdx.ExecuteNonQuery();
                    if (resultIdx != 0) {
                        throw new Exception("Creating the index failed.");
                    }
                }
            }
        }

        public void MakeTableForListOfFieldsIfNotExists(bool idxOnName)
        {
            if (Fields.Count == 0) {
                throw new Exception("No fields defined for database");
            }

            var sql = "CREATE TABLE IF NOT EXISTS " + TableName + "(";
            foreach (var field in Fields) {
                sql += field.Name + " " + field.Type + ",";
            }

            sql = sql.Substring(0, sql.Length - 1) + ");";
            var indexCmd = "CREATE INDEX if not EXISTS " + TableName + "_idx ON " + TableName + "(Name);";

            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = sql;
                //var result =
                command.ExecuteNonQuery();
                if (idxOnName) {
                    var command1 = conn.CreateCommand();
                    command1.CommandText = indexCmd;
                    //var result =
                    command1.ExecuteNonQuery();
                }
            }
        }

        [ItemNotNull]
        [NotNull]
        public IEnumerable<T> ReadEntireTableDBAsEnumerable([CanBeNull] string orderByField = null)
        {
            var query = "select MessagePack from " + TableName;
            if (orderByField != null) {
                query += " ORDER BY " + orderByField;
            }

            using (var con = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                using (var cmd = new SQLiteCommand(con)) {
                    cmd.CommandText = query;
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        var p = ReadSingleLine(reader);
                        yield return p;
                    }

                    reader.Close();
                    con.Close();
                }
            }
        }

        [NotNull]
        public T ReadSingleLine([NotNull] [ItemNotNull] SQLiteDataReader reader)
        {
            try {
                var messagePack = (byte[])reader["MessagePack"];
                T p = LZ4MessagePackSerializer.Deserialize<T>(messagePack);
                return p;
            }
            catch (Exception ex) {
                MyLogger.ErrorM(ex.Message, Stage.Preparation, "BasicSaveable.ReadSingleLine" + typeof(T).Name);
                throw;
            }
        }

        [ItemNotNull]
        [NotNull]
        public IEnumerable<T> ReadSubsetOfTableDBAsEnumerable([NotNull] string whereField,
                                                              [NotNull] string condition,
                                                              [CanBeNull] string orderByField = null)
        {
            var query = "select MessagePack from " + TableName + " WHERE " + whereField + " = @par1";
            if (orderByField != null) {
                query += " ORDER BY " + orderByField;
            }

            using (var con = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                using (var cmd = new SQLiteCommand(con)) {
                    cmd.CommandText = query;
                    cmd.Parameters.AddWithValue("@par1", condition);
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        var p = ReadSingleLine(reader);
                        yield return p;
                    }

                    reader.Close();
                    con.Close();
                }
            }
        }

        public void SaveDictionaryToDatabase([NotNull] ILogger logger)
        {
            if (RowEntries.Count == 0) {
                return;
            }

            if (_namesInDatabase == null) {
                RefreshNamesInDatabase(logger);
            }

            //figure out sql
            var firstrow = RowEntries[0];
            var sql = "Insert into " + TableName + "(";
            var fields = "";
            var parameters = "";
            foreach (var pair in firstrow) {
                fields += pair.Name + ",";
                parameters += "@" + pair.Name + ",";
            }

            fields = fields.Substring(0, fields.Length - 1);
            parameters = parameters.Substring(0, parameters.Length - 1);
            sql += fields + ") VALUES (" + parameters + ")";
            //execute the sql
            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                using (var transaction = conn.BeginTransaction()) {
                    using (var command = conn.CreateCommand()) {
                        command.CommandText = sql;
                        foreach (var row in RowEntries) {
                            if (row.Count != firstrow.Count) {
                                throw new Exception("Incorrect number of columns");
                            }

                            command.Parameters.Clear();
                            foreach (var pair in row) {
                                var parameter = "@" + pair.Name;
                                if (pair.Name == "Name") {
                                    if (_namesInDatabase == null) {
                                        throw new FlaException("names as null");
                                    }

                                    _namesInDatabase.Add((string)pair.Value);
                                }

                                command.Parameters.AddWithValue(parameter, pair.Value);
                            }

                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }

            RowEntries.Clear();
        }

        [ItemNotNull]
        [NotNull]
        public List<T1> SelectSingleDistinctField<T1>([NotNull] string field)
        {
            var query = "select distinct " + field + " from " + TableName;
            List<T1> result = new List<T1>();
            using (var con = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                using (var cmd = new SQLiteCommand(con)) {
                    cmd.CommandText = query;
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        var res = (T1)reader[field];
                        result.Add(res);
                    }

                    reader.Close();
                    con.Close();
                }
            }

            return result;
        }

        private void RefreshNamesInDatabase([NotNull] ILogger logger)
        {
            logger.Debug("Refreshing names in database", Stage.ProfileGeneration, "SavableEntry");
            _namesInDatabase = new HashSet<string>();
            using (var conn = new SQLiteConnection(SqliteDB.GetConnectionstring())) {
                conn.Open();
                var command1 = conn.CreateCommand();
                command1.CommandText = "SELECT Name FROM " + TableName;
                var reader = command1.ExecuteReader();
                while (reader.Read()) {
                    var name = (string)reader["Name"];
                    // ReSharper disable once PossibleNullReferenceException
                    if (!_namesInDatabase.Contains(name)) {
                        _namesInDatabase.Add(name);
                    }
                    else {
                        logger.ErrorM("Found more than one entry for the key " + name + ", probably left over bugs, deleting and reimporting.",
                            Stage.ProfileGeneration,
                            "SavableEntry");
                        DeleteEntryByName(name);
                    }
                }
            }

            logger.Info("Finished refreshing names in database", Stage.ProfileGeneration, "SavableEntry");
        }
    }
}