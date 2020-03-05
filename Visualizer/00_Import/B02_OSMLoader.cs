using System;
using System.IO;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Visualizer.OSM;

namespace BurgdorfStatistics._00_Import {
    // ReSharper disable once InconsistentNaming
    public class B02_OsmLoader : RunableWithBenchmark {
        public B02_OsmLoader([NotNull] ServiceRepository services)
            : base(nameof(B02_OsmLoader), Stage.Raw, 102, services, false)
        {
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<OsmFeature>(Stage.Raw, Constants.PresentSlice);
            var db = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            const string jsonfile = "U:\\SimZukunft\\RawDataForMerging\\roadsBurgdorf.geojson";
            var json = File.ReadAllText(jsonfile);
            var collection1 = JsonConvert.DeserializeObject<FeatureCollection>(json);

            const string jsonfileBuildings = "U:\\SimZukunft\\RawDataForMerging\\buildingsBurgdorf.geojson";
            var jsonBuildings = File.ReadAllText(jsonfileBuildings);
            var collection2 = JsonConvert.DeserializeObject<FeatureCollection>(jsonBuildings);

            collection1.Features.AddRange(collection2.Features);
            db.Database.BeginTransaction();
            foreach (var feature in collection1.Features) {
                var osmf = new OsmFeature( feature, Guid.NewGuid().ToString());
                if (feature.Geometry.Type == GeoJSONObjectType.Polygon) {
                    var p = (Polygon)feature.Geometry;
                    var ls = p.Coordinates[0];
                    foreach (var coordinate in ls.Coordinates) {
                        var wp = new WgsPoint(coordinate.Longitude, coordinate.Latitude);
                        osmf.WgsPoints.Add(wp);
                    }
                }

                db.Database.Save(osmf);
            }

            db.Database.CompleteTransaction();
        }
    }
}