using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Common.Steps;
using Data.DataModel;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace FutureLoadAnalyzerLib._00_Import {
    [TableName(nameof(B05_SonnendachGeoJson))]
    [Table(nameof(B05_SonnendachGeoJson))]
    [NPoco.PrimaryKey(nameof(ID))]
    // ReSharper disable once InconsistentNaming
    public class B05_SonnendachGeoJson {

        [Obsolete("For json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public B05_SonnendachGeoJson()
        {
        }

        public B05_SonnendachGeoJson([JetBrains.Annotations.NotNull] Feature feature,  [JetBrains.Annotations.NotNull] string guid)
        {
            Feature = feature;
            Guid = guid;
        }

        [SQLite.Ignore]
        [NPoco.Ignore]
        [JetBrains.Annotations.NotNull]
        public Feature Feature { get; set; }

        [JetBrains.Annotations.NotNull]
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

    // ReSharper disable once InconsistentNaming
    public class B05_SonnendachGeoJsonLoader : RunableWithBenchmark {
        public B05_SonnendachGeoJsonLoader([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(B05_SonnendachGeoJsonLoader), Stage.Raw, 105, services, false)
        {
        }


        protected override void RunActualProcess()
        {
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<B05_SonnendachGeoJson>();
            string jsonfile = CombineForRaw("sonnendach8.geojson");
            var json = File.ReadAllText(jsonfile);
            var collection1 = JsonConvert.DeserializeObject<FeatureCollection>(json);

            db.BeginTransaction();
            foreach (var feature in collection1.Features) {
                var osmf = new B05_SonnendachGeoJson(feature, Guid.NewGuid().ToString());
                if (feature.Geometry.Type == GeoJSONObjectType.Polygon) {
                    var p = (Polygon)feature.Geometry;
                    foreach (var lineString in p.Coordinates) {
                        foreach (var coordinate in lineString.Coordinates) {
                            var wp = new WgsPoint(coordinate.Longitude, coordinate.Latitude);
                            osmf.WgsPoints.Add(wp);
                        }
                    }
                }

                if (feature.Geometry.Type == GeoJSONObjectType.MultiPolygon) {
                    var p = (MultiPolygon)feature.Geometry;
                    foreach (var polygon in p.Coordinates) {
                        foreach (var lineString in polygon.Coordinates) {
                            foreach (var coordinate in lineString.Coordinates) {
                                var wp = WgsPoint.ConvertKoordsToLonLat(coordinate.Longitude, coordinate.Latitude);
                                osmf.WgsPoints.Add(wp);
                            }
                        }
                    }
                }

                db.Save(osmf);
            }

            db.CompleteTransaction();
        }
    }
}