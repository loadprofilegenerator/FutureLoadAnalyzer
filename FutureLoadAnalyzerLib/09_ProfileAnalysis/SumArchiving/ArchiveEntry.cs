using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using Common.Database;
using Common.Logging;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using MessagePack;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving {
    [MessagePackObject]
    public class ArchiveEntry:BasicSaveable<ArchiveEntry> {
        [Obsolete("Only json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public ArchiveEntry()
        {

        }

        public ArchiveEntry([NotNull] string name, AnalysisKey key, [NotNull] Profile profile, GenerationOrLoad generationOrLoad, [NotNull] string trafokreis)
        {
            Name = name;
            Key = key;
            Profile = profile;
            GenerationOrLoad = generationOrLoad;
            Trafokreis = trafokreis;
        }

        [NotNull]
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public AnalysisKey Key { get; set; }

        [NotNull]
        [Key(2)]
        public Profile Profile { get; set; }
        [Key(3)]
        public GenerationOrLoad GenerationOrLoad { get; set; }
        [NotNull]
        [Key(4)]
        public string Trafokreis { get; set; }
        [NotNull]
        public static ArchiveEntry ReadSingleLine([NotNull] [ItemNotNull]
                                                  SQLiteDataReader reader, [NotNull] ILogger logger)
        {
            try {
                var messagePack = (byte[])reader["MessagePack"];
                ArchiveEntry p = LZ4MessagePackSerializer.Deserialize<ArchiveEntry>(messagePack);
                return p;
            }
            catch (Exception ex) {
                logger.ErrorM(ex.Message, Stage.Preparation, "Profile.ReadSingleLine");
                throw;
            }
        }
        [NotNull]
        [ItemNotNull]
        public static List<ArchiveEntry> Load([NotNull] MyDb db, [NotNull] ILogger logger)
        {
            const string query = "select * from " + nameof(ArchiveEntry);
            List<ArchiveEntry> entries = new List<ArchiveEntry>();
            using (var con = new SQLiteConnection(db.GetConnectionstring())) {
                var cmd = new SQLiteCommand(con) {
                    CommandText = query
                };
                con.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    var e = ReadSingleLine(reader, logger);
                    entries.Add(e);
                }
                reader.Close();
                con.Close();
            }
            return entries;
        }

        protected override void SetAdditionalFieldsForRow([NotNull] RowBuilder rb)
        {
            rb.Add("Name", Name);
            rb.Add("Trafokreis",Trafokreis);
        }

        protected override void SetFieldListToSaveOtherThanMessagePack([NotNull] Action<string, SqliteDataType> addField)
        {
            addField("Name", SqliteDataType.Text);
            addField("Trafokreis", SqliteDataType.Text);
        }
    }
}