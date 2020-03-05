using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Config;
using Common.Database;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NPoco;
using SQLite;

namespace Common.ResultFiles {
    [TableName(nameof(FlaResultFileEntry))]
    [Table(nameof(FlaResultFileEntry))]
    [NPoco.PrimaryKey(nameof(ID))]
    public class FlaResultFileEntry {
        public FlaResultFileEntry([JetBrains.Annotations.NotNull] string section,
                                  [JetBrains.Annotations.NotNull] string sectionDescription,
                                  [JetBrains.Annotations.NotNull] string fullFilename,
                                  [JetBrains.Annotations.NotNull] string fileTitle,
                                  [JetBrains.Annotations.NotNull] string fileDescription,
                                  [JetBrains.Annotations.NotNull] ScenarioSliceParameters slice,
                                  Stage srcStage)
        {
            Section = section;
            SectionDescription = sectionDescription;
            FullFilename = fullFilename;
            FileTitle = fileTitle;
            FileDescription = fileDescription;
            Scenario = slice.DstScenario;
            Year = slice.DstYear;
            SrcStage = srcStage;
        }

        // ReSharper disable once NotNullMemberIsNotInitialized
        public FlaResultFileEntry()
        {
        }

        [CanBeNull]

        public string FileDescription { get; set; }

        [CanBeNull]

        public string FileTitle { get; set; }

        [JetBrains.Annotations.NotNull]
        public string FullFilename { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JetBrains.Annotations.NotNull]
        public Scenario Scenario { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ScenarioStr {
            get => Scenario.ToString();
            // ReSharper disable once ValueParameterNotUsed
#pragma warning disable S108 // Nested blocks of code should not be left empty
            set { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
        }

        [JetBrains.Annotations.NotNull]
        public string Section { get; set; }

        [JetBrains.Annotations.NotNull]
        public string SectionDescription { get; set; }

        public Stage SrcStage { get; set; } = Stage.Unknown;

        [JetBrains.Annotations.NotNull]
        public string SrcStageStr {
            get => SrcStage.ToString();
            // ReSharper disable once UnusedMember.Global
            // ReSharper disable once ValueParameterNotUsed
#pragma warning disable S108 // Nested blocks of code should not be left empty
            set { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
        }

        public int Year { get; set; }

        public static void ClearStage(Stage stage, [JetBrains.Annotations.NotNull] RunningConfig config)
        {
            var mysql = new SqlConnectionPreparer(config);
            var db = mysql.GetDatabaseConnection(Stage.Reporting, Constants.PresentSlice);
            db.CreateTableIfNotExists<FlaResultFileEntry>();
            var entries = db.Fetch<FlaResultFileEntry>().Where(x => x.SrcStage == stage || x.SrcStage == Stage.Unknown);
            foreach (var entry in entries) {
                db.Delete(entry);
            }
        }

        [JetBrains.Annotations.NotNull]
        public string GetUniqueFilename()
        {
            FileInfo fi = new FileInfo(FullFilename);
            string s = Section + "_" + ScenarioStr + "_" + Year + "_" + SrcStageStr;
            return s + "." + fi.Name;
        }

        [ItemNotNull]
        [JetBrains.Annotations.NotNull]
        public static List<FlaResultFileEntry> LoadAllForScenario([CanBeNull] Scenario scenario, [JetBrains.Annotations.NotNull] RunningConfig config)
        {
            var mysql = new SqlConnectionPreparer(config);
            var db = mysql.GetDatabaseConnection(Stage.Reporting, Constants.PresentSlice);
            db.CreateTableIfNotExists<FlaResultFileEntry>();
            var entries = db.Fetch<FlaResultFileEntry>();
            if (scenario == null) {
                return entries;
            }

            return entries.Where(x => x.Scenario == scenario).ToList();
        }
    }
}