using System.Collections.Generic;
using System.Data.SQLite;
using Automation.ResultFiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class FileEntryLoader {
        [NotNull]
        public Dictionary<HouseholdKey, FileEntry> Files { get; } = new Dictionary<HouseholdKey, FileEntry>();
        public void LoadFiles([NotNull] string generalResultName)
        {
            const string sql = "SELECT * FROM DatabaseList";

            string constr = "Data Source="+ generalResultName +
                            ";Version=3";
            using (var conn =
                new SQLiteConnection(constr)) {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand()) {
                    cmd.Connection = conn;

                    cmd.CommandText = sql;
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        string keyStr = reader["HouseholdKey"].ToString();
                        string filename = reader["Filename"].ToString();
                        HouseholdKey key = new HouseholdKey(keyStr);
                        FileEntry fe = new FileEntry(keyStr,filename);
                        if (!Files.ContainsKey(key)) {
                            Files.Add(key, fe);
                        }
                    }
                }
                conn.Close();
            }
        }
    }
}