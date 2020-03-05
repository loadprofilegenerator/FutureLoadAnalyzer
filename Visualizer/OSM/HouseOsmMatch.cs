using System;
using System.Diagnostics.CodeAnalysis;
using NPoco;
using SQLite;

namespace Visualizer.OSM {
    [TableName(nameof(HouseOsmMatch))]
    [Table(nameof(HouseOsmMatch))]
    [NPoco.PrimaryKey(nameof(ID))]
    public class HouseOsmMatch {
        public HouseOsmMatch([JetBrains.Annotations.NotNull] string houseGuid, [JetBrains.Annotations.NotNull] string osmGuid, MatchType matchType, double distance)
        {
            HouseGuid = houseGuid;
            OsmGuid = osmGuid;
            MatchType = matchType;
            Distance = distance;
        }

        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public HouseOsmMatch()
        {
        }

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string OsmGuid { get; set; }
        public MatchType MatchType { get; set; }
        public double Distance { get; set; }
    }
}