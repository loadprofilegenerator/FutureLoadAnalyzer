using System.Collections.Generic;
using System.Data.SQLite;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public class LPGReader {
        [ItemNotNull]
        [NotNull]
        public List<T> ReadFromJson<T>([NotNull] string tableName, [NotNull] string filename)
        {
            string sql = "SELECT json FROM " + tableName;
            string constr = "Data Source=" + filename + ";Version=3";
            using (var conn = new SQLiteConnection(constr)) {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand()) {
                    cmd.Connection = conn;
                    cmd.CommandText = sql;
                    var reader = cmd.ExecuteReader();
                    List<T> resultsObjects = new List<T>();
                    while (reader.Read()) {
                        string jsonstr = reader[0].ToString();
                        T re = JsonConvert.DeserializeObject<T>(jsonstr);
                        resultsObjects.Add(re);
                    }

                    conn.Close();
                    return resultsObjects;
                }
            }
        }
    }
}
