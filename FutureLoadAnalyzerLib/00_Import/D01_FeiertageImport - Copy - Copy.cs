using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class FeiertagImport {
        [NotNull]
        public string Name { get; set; }
        [NotNull]
        public string DateAsJson {
            get => JsonConvert.SerializeObject(Date, Formatting.Indented);
            set => Date = JsonConvert.DeserializeObject<DateTime>(value);
        }
        [NPoco.Ignore]
        [SQLite.Ignore]
        public DateTime Date { get; set; }

        [Obsolete("for json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public FeiertagImport()
        {
        }
        public int ID { get; set; }
        public FeiertagImport([NotNull] string bezeichnung, DateTime date)
        {
            Name = bezeichnung;
            Date = date;
        }
    }
    // ReSharper disable once InconsistentNaming
}