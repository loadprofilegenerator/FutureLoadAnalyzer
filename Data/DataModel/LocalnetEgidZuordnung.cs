using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel {
    [TableName(nameof(LocalnetEgidZuordnung))]
    [Table(nameof(LocalnetEgidZuordnung))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LocalnetEgidZuordnung {
        public LocalnetEgidZuordnung([JetBrains.Annotations.NotNull] string komplexName, [JetBrains.Annotations.NotNull] string bfhEgidsAusGWR, [JetBrains.Annotations.NotNull] string status,
                                     [JetBrains.Annotations.NotNull] string gebäudeObjektIDs)
        {
            KomplexName = komplexName;
            BFHEgidsAusGWR = bfhEgidsAusGWR;
            LocalnetEGids = new List<long>();
            Status = status;
            GebäudeObjektIDs = gebäudeObjektIDs;
        }

        [SQLite.PrimaryKey]
        [AutoIncrement]

        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string KomplexName { get; set; }

        [JetBrains.Annotations.NotNull]
        public string BFHEgidsAusGWR { get; set; }

        [JetBrains.Annotations.NotNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        public List<long> LocalnetEGids { get; set; }

        [JetBrains.Annotations.NotNull]
        [UsedImplicitly]
        public string LocalnetEGidsAsJson {
            get => JsonConvert.SerializeObject(LocalnetEGids, Formatting.None);
            set => LocalnetEGids = JsonConvert.DeserializeObject<List<long>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string GISHausanschlussadresse { get; set; } = "";

        [JetBrains.Annotations.NotNull]
        public string Status { get; set; }

        [JetBrains.Annotations.NotNull]
        public string GebäudeObjektIDs { get; set; }
    }
}