using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;
using Feature = GeoJSON.Net.Feature.Feature;

namespace Visualizer.OSM {
    [TableName(nameof(OsmFeature))]
    [Table(nameof(OsmFeature))]
    [NPoco.PrimaryKey(nameof(ID))]
    public class OsmFeature {
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public OsmFeature()
        {
        }

        public OsmFeature([JetBrains.Annotations.NotNull] Feature feature, [JetBrains.Annotations.NotNull] string guid)
        {
            Feature = feature;
            Guid = guid;
        }

        public override string ToString() => Guid + " - " + JsonConvert.SerializeObject(Feature.Properties);

        [SQLite.Ignore]
        [NPoco.Ignore]
        [JetBrains.Annotations.NotNull]
        public Feature Feature { get; set; }

        [CanBeNull]
        public string FeatureAsJson {
            get => JsonConvert.SerializeObject(Feature, Formatting.Indented);
            set => Feature = JsonConvert.DeserializeObject<Feature>(value);
        }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.Ignore]
        [NPoco.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<WgsPoint> WgsPoints { get; set; } = new List<WgsPoint>();

        [JetBrains.Annotations.NotNull]
        public string WgsAsJson {
            get => JsonConvert.SerializeObject(WgsPoints, Formatting.Indented);
            set => WgsPoints = JsonConvert.DeserializeObject<List<WgsPoint>>(value);
        }
    }
}