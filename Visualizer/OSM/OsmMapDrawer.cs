using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Common;
using Common.Config;
using Common.Database;
using Common.Logging;
using Common.Steps;
using Data.DataModel;
using GeoJSON.Net;
using GeoJSON.Net.Geometry;
using JetBrains.Annotations;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Logger = Mapsui.Logging.Logger;
using Point = Mapsui.Geometries.Point;
using Polygon = Mapsui.Geometries.Polygon;

namespace Visualizer.OSM {
    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
    public class OsmMapDrawer : BasicLoggable {
        [NotNull] private readonly Rectangle _boundingBox = new Rectangle(47.0258609, 7.6499145, 7.5873523, 47.07347);
        [NotNull] private readonly RunningConfig _config;

        [CanBeNull] private Dictionary<string, List<string>> _houseOsmMatches;
        [CanBeNull] [ItemCanBeNull] private List<OsmFeature> _osmFeaturesList;

        public OsmMapDrawer([NotNull] ILogger logger, [NotNull] RunningConfig config) : base(logger, Stage.Plotting, nameof(OsmMapDrawer)) =>
            _config = config;

        [NotNull]
        private Dictionary<string, List<string>> HouseOsmMatches {
            get {
                if (_houseOsmMatches == null) {
                    _houseOsmMatches = new Dictionary<string, List<string>>();
                    SqlConnectionPreparer sqlConnectionPreparer = new SqlConnectionPreparer(_config);
                    var dbRaw = sqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
                    var housematches = dbRaw.Fetch<HouseOsmMatch>();
                    var assignedOsmGuids = new HashSet<string>();
                    _houseOsmMatches = new Dictionary<string, List<string>>();
                    foreach (var match in housematches) {
                        if (match.MatchType != MatchType.GWRClosest && match.MatchType != MatchType.GWRMatch &&
                            match.MatchType != MatchType.LocalnetClosest && match.MatchType != MatchType.LocalnetMatch) {
                            continue;
                        }

                        if (!_houseOsmMatches.ContainsKey(match.HouseGuid)) {
                            _houseOsmMatches.Add(match.HouseGuid, new List<string>());
                        }

                        if (!_houseOsmMatches[match.HouseGuid].Contains(match.OsmGuid)) {
                            _houseOsmMatches[match.HouseGuid].Add(match.OsmGuid);
                            if (!assignedOsmGuids.Contains(match.OsmGuid)) {
                                assignedOsmGuids.Add(match.OsmGuid);
                            }
                        }
                    }
                }

                return _houseOsmMatches;
            }
        }

        [NotNull]
        [ItemNotNull]
        private List<OsmFeature> OsmFeatures {
            get {
                if (_osmFeaturesList == null) {
                    SqlConnectionPreparer ms = new SqlConnectionPreparer(_config);

                    var dbRaw = ms.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
                    _osmFeaturesList = dbRaw.Fetch<OsmFeature>();
                }

                return _osmFeaturesList;
            }
        }

        [NotNull]
        public MemoryProvider CreatePolygonProvider([NotNull] [ItemNotNull] List<OsmFeature> osmFeatures,
                                                    [NotNull] [ItemNotNull] List<MapColorEntryWithOsmGuid> colors,
                                                    [NotNull] [ItemNotNull] List<WgsPoint> additionalPoints,
                                                    [NotNull] [ItemNotNull] List<MapLegendEntry> mapLabels,
                                                    [NotNull] [ItemNotNull] List<LineEntry> lines)
        {
            var maxPoly = new Rectangle(50, 3, 9, 45); // definitly bigger than burgdorf
            var mapsuiFeatures = new List<Feature>();
            foreach (var osmFeature in osmFeatures) {
                if (osmFeature.Feature.Geometry.Type == GeoJSONObjectType.Polygon) {
                    var color = colors.FirstOrDefault(x => x.OsmGuid == osmFeature.Guid);
                    mapsuiFeatures.Add(DrawHouse(osmFeature, color, maxPoly));
                }
                else if (osmFeature.Feature.Geometry.Type == GeoJSONObjectType.LineString) {
                    mapsuiFeatures.Add(DrawRoad(osmFeature));
                }
            }

            Info(maxPoly.ToString());
            var myfont = new Font {Size = 10, FontFamily = "Arial"};
            foreach (LineEntry line in lines) {
                const int size = 2;
                var feature = new Mapsui.Providers.Feature();
                var mapsuiPointStart = Mercator.FromLonLat(line.StartPoint.Lon, line.StartPoint.Lat);
                var mapsuiPointEnd = Mercator.FromLonLat(line.EndPoint.Lon, line.EndPoint.Lat);
                var polygon = new Polygon();
                polygon.ExteriorRing.Vertices.Add(mapsuiPointStart.Offset(size, size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPointStart.Offset(-1 * size, 1 * size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPointEnd.Offset(1 * size, 1 * size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPointEnd.Offset(1 * size, -1 * size));
                feature.Styles.Add(new LabelStyle {
                    Text = line.Text,
                    BackColor = new Brush(Color.Gray),
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    Font = myfont
                });
                feature.Geometry = polygon;
                feature.Styles.Add(new VectorStyle {
                    Enabled = true,
                    Fill = CreateBrush(Constants.Blue.GetMapSuiColor(), FillStyle.Solid),
                    Outline = CreatePen(Constants.Blue.GetMapSuiColor(), 1, PenStyle.Solid)
                    //Line = CreatePen(Color.Black, 10, PenStyle.Solid)
                });
                mapsuiFeatures.Add(feature);
            }

            foreach (var wgsPoint in additionalPoints) {
                if (_boundingBox.IsOutside(wgsPoint)) {
                    throw new FlaException("Invalid point");
                }

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0068 // Use recommended dispose pattern
                var feature = new Feature();
#pragma warning restore IDE0068 // Use recommended dispose pattern
#pragma warning restore CA2000 // Dispose objects before losing scope
                var mapsuiPoint = Mercator.FromLonLat(wgsPoint.Lon, wgsPoint.Lat);
                var polygon = new Polygon();
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(-1 * wgsPoint.Size, -1 * wgsPoint.Size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(-1 * wgsPoint.Size, 1 * wgsPoint.Size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(1 * wgsPoint.Size, 1 * wgsPoint.Size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(1 * wgsPoint.Size, -1 * wgsPoint.Size));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(-1 * wgsPoint.Size, -1 * wgsPoint.Size));

                feature.Geometry = polygon;
                if (wgsPoint.Rgb == null) {
                    throw new FlaException("rgb was null");
                }

                feature.Styles.Add(new VectorStyle {
                    Enabled = true,
                    Fill = CreateBrush(wgsPoint.Rgb.GetMapSuiColor(), FillStyle.Solid),
                    Outline = CreatePen(wgsPoint.Rgb.GetMapSuiColor(), 1, PenStyle.Solid)
                    //Line = CreatePen(Color.Black, 10, PenStyle.Solid)
                });
                mapsuiFeatures.Add(feature);
            }

            var edge = new WgsPoint(_boundingBox.Bottom, _boundingBox.Left + 0.002);
            const int legendSize = 5;
            foreach (var mpLabel in mapLabels) {
                var legendLabel = new Feature();
                var mapsuiPoint = Mercator.FromLonLat(edge.Lon, edge.Lat);
                var polygon = new Mapsui.Geometries.Polygon();
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(-1 * legendSize, -1 * legendSize));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(-1 * legendSize, 1 * legendSize));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(1 * legendSize, 1 * legendSize));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(1 * legendSize, -1 * legendSize));
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint.Offset(-1 * legendSize, -1 * legendSize));

                legendLabel.Geometry = polygon;
                legendLabel.Styles.Add(new VectorStyle {
                    Enabled = true,
                    Fill = CreateBrush(mpLabel.Rgb.GetMapSuiColor(), FillStyle.Solid),
                    Outline = CreatePen(mpLabel.Rgb.GetMapSuiColor(), 1, PenStyle.Solid),
                    Line = CreatePen(mpLabel.Rgb.GetMapSuiColor(), 10, PenStyle.Solid)
                });
                var f = new Font {
                    FontFamily = "Arial",
                    Size = 64
                };
                legendLabel.Styles.Add(new LabelStyle {
                    Text = mpLabel.Name,
                    BackColor = new Brush(Color.White),
                    ForeColor = mpLabel.Rgb.GetMapSuiColor(),
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                    Font = f
                });
                mapsuiFeatures.Add(legendLabel);
                edge.Lat += 0.0015;
            }

            var filteredFeatures = mapsuiFeatures.Where(x => x != null).ToList();
            var provider = new MemoryProvider(filteredFeatures);

            return provider;
        }

        public void MakeMap([NotNull] string dstFileName,
                            [NotNull] [ItemNotNull] List<MapColorEntryWithHouseGuid> colors,
                            [NotNull] [ItemNotNull] List<WgsPoint> additionalPoints,
                            [NotNull] [ItemNotNull] List<MapLegendEntry> mapLabels,
                            [NotNull] [ItemNotNull] List<LineEntry> lines)
        {
            if (!dstFileName.Contains(".png")) {
                throw new FlaException("Invalid file name");
            }

            if (!dstFileName.Contains("\\")) {
                throw new FlaException("Invalid file name");
            }

            if (!dstFileName.Contains(":")) {
                throw new FlaException("Invalid file name");
            }

            var osmGuids = new List<MapColorEntryWithOsmGuid>();
            foreach (var mapColorEntryWithHouseGuid in colors) {
                if (HouseOsmMatches.ContainsKey(mapColorEntryWithHouseGuid.HouseGuid)) {
                    var osmGuidList = HouseOsmMatches[mapColorEntryWithHouseGuid.HouseGuid];
                    foreach (var osmGuid in osmGuidList) {
                        var col = mapColorEntryWithHouseGuid.DesiredColor;
                        if (col.R == 255 && col.G == 255 && col.B == 255) {
                            throw new FlaException("white is not a good color on a white background");
                        }

                        osmGuids.Add(new MapColorEntryWithOsmGuid(osmGuid, col, mapColorEntryWithHouseGuid.Text));
                    }
                }
            }

            MakeMap(dstFileName, osmGuids, additionalPoints, mapLabels, lines);
        }

        public void MakeMap([NotNull] string dstFileName,
                            [ItemNotNull] [NotNull] List<MapColorEntryWithOsmGuid> colors,
                            [NotNull] [ItemNotNull] List<WgsPoint> additionalPoints,
                            [NotNull] [ItemNotNull] List<MapLegendEntry> mapLabels,
                            [NotNull] [ItemNotNull] List<LineEntry> lines)
        {
            var map1 = new Map {BackColor = Color.White, Home = n => n.NavigateTo(new Point(0, 0), 63000)};

            var layer = new MemoryLayer {
                DataSource = CreatePolygonProvider(OsmFeatures, colors, additionalPoints, mapLabels, lines),
                Name = "Polygon"
            };
            map1.Layers.Add(layer);
            {
                //full resolution
                const int resolutionMultiplier = 2;
                var viewport = new Viewport {
                    Center = map1.Envelope.Centroid,
                    Width = 6000 * resolutionMultiplier,
                    Height = 6000 * resolutionMultiplier,
                    Resolution = 1.5 / resolutionMultiplier
                };
                // act

                if (map1.Layers == null) {
                    throw new FlaException("map1.layers was null");
                }

                Logger.LogDelegate = MapSuiLogger;

                var bitmap = new MapRenderer().RenderToBitmapStream(viewport, map1.Layers, map1.BackColor);

                if (bitmap == null) {
                    throw new FlaException("maprenderer render to Bitmap was null. Current directory was : " + Directory.GetCurrentDirectory());
                }

                using (var fileStream = new FileStream(dstFileName, FileMode.Create, FileAccess.Write)) {
                    bitmap.WriteTo(fileStream);
                }
            }
            {
                //reduced resolution
                var dstFileName2 = dstFileName.Replace(".png", ".s.png");

                const int resolutionMultiplier = 1;
                var viewport = new Viewport {
                    Center = map1.Envelope.Centroid,
                    Width = 3000 * resolutionMultiplier,
                    Height = 3000 * resolutionMultiplier,
                    Resolution = 2.5 / resolutionMultiplier
                };
                // act
                var bitmap = new MapRenderer().RenderToBitmapStream(viewport, map1.Layers, map1.BackColor);
                using (var fileStream = new FileStream(dstFileName2, FileMode.Create, FileAccess.Write)) {
                    bitmap.WriteTo(fileStream);
                }
            }
        }

        [NotNull]
        private static Brush CreateBrush([NotNull] Color color, FillStyle fillStyle, [CanBeNull] int? imageId = null)
        {
            if (imageId.HasValue && !(fillStyle == FillStyle.Bitmap || fillStyle == FillStyle.BitmapRotated)) {
                fillStyle = FillStyle.Bitmap;
            }

            var brush = new Brush {
                FillStyle = fillStyle,
                BitmapId = imageId ?? -1,
                Color = color
            };

            return brush;
        }


        [NotNull]
        private static Mapsui.Styles.Pen CreatePen([NotNull] Color color, int width, PenStyle penStyle) =>
            new Pen(color, width) {PenStyle = penStyle};

        [NotNull]
        private Feature DrawHouse([NotNull] OsmFeature osmFeature, [CanBeNull] MapColorEntryWithOsmGuid mapColor, [NotNull] Rectangle boundingRec)
        {
            var feature = new Mapsui.Providers.Feature();
            var polygon = new Polygon();
            var geoPoly = (GeoJSON.Net.Geometry.Polygon)osmFeature.Feature.Geometry;
            var ls = geoPoly.Coordinates[0];
            var myfont = new Font {Size = 10, FontFamily = "Arial"};
            foreach (var lsCoordinate in ls.Coordinates) {
                UpdateGlobalBoundingBox(boundingRec, lsCoordinate);
                if (_boundingBox.IsOutside(new WgsPoint(lsCoordinate.Longitude, lsCoordinate.Latitude))) {
                    throw new FlaException("Invalid point");
                }

                var mapsuiPoint = Mercator.FromLonLat(lsCoordinate.Longitude, lsCoordinate.Latitude);
                polygon.ExteriorRing.Vertices.Add(mapsuiPoint);
            }

            feature.Geometry = polygon;
            if (mapColor == null) {
                feature.Styles.Add(new VectorStyle {
                    Enabled = true,
                    Fill = CreateBrush(Color.Red, FillStyle.Solid),
                    Outline = CreatePen(Color.Black, 1, PenStyle.Solid),
                    Line = null
                });
            }
            else {
                feature.Styles.Add(new VectorStyle {
                    Enabled = true,
                    Fill = CreateBrush(mapColor.DesiredColor.GetMapSuiColor(), FillStyle.Solid),
                    Outline = CreatePen(Color.Black, 1, PenStyle.Solid),
                    Line = null
                });
                if (!string.IsNullOrWhiteSpace(mapColor.Text)) {
                    feature.Styles.Add(new LabelStyle {
                        Text = mapColor.Text,
                        BackColor = new Brush(Color.Gray),
                        HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                        Font = myfont
                    });
                }
            }

            return feature;
        }

        [CanBeNull]
        private Feature DrawRoad([NotNull] OsmFeature osmFeature)
        {
            var feature = new Mapsui.Providers.Feature();
            if (!osmFeature.Feature.Properties.ContainsKey("highway")) {
                return null;
            }


            var highwaytyp = (string)osmFeature.Feature.Properties["highway"];
            var pen = GetPenForHighway(highwaytyp);
            if (pen == null) {
                return null;
            }

            var ls = (LineString)osmFeature.Feature.Geometry;
            var points = new List<Point>();
            foreach (var lsCoordinate in ls.Coordinates) {
                if (!_boundingBox.IsOutside(new WgsPoint(lsCoordinate.Longitude, lsCoordinate.Latitude))) {
                    var mapsuiPoint = Mercator.FromLonLat(lsCoordinate.Longitude, lsCoordinate.Latitude);
                    points.Add(mapsuiPoint);
                }
            }

            var linestring = new Mapsui.Geometries.LineString(points);

            feature.Geometry = linestring;
            feature.Styles.Add(new VectorStyle {
                Enabled = true,
                //Fill = CreateBrush(Color.Blue, FillStyle.Solid),
                Outline = CreatePen(Color.Black, 5, PenStyle.Solid),
                Line = pen
            });
            return feature;
        }

        [CanBeNull]
        private Pen GetPenForHighway([NotNull] string highway)
        {
            switch (highway) {
                case "service":
                    return CreatePen(Color.Gray, 10, PenStyle.Solid);
                case "platform":
                    return CreatePen(Color.Gray, 1, PenStyle.Solid);
                case "footway":
                    return null;
                case "path":
                    return null;
                case "cycleway":
                    return null;
                case "bridleway":
                    return null;
                case "track":
                    return null;
                case "steps":
                    return null;
                case "unclassified":
                    return null;
                case "pedestrian":
                    return CreatePen(Color.Gray, 1, PenStyle.Solid);

                case "living_street":
                    return CreatePen(Color.Gray, 1, PenStyle.Solid);
                case "secondary":
                    return CreatePen(Color.Gray, 10, PenStyle.Solid);
                case "tertiary":
                    return CreatePen(Color.Gray, 5, PenStyle.Solid);

                case "primary":
                    return CreatePen(Color.FromArgb(1000, 250, 250, 150), 15, PenStyle.Solid);
                case "residential":
                    return CreatePen(Color.Gray, 5, PenStyle.Solid);

                default:
                    Info(highway);
                    return CreatePen(Color.Orange, 1, PenStyle.Solid);
            }
        }

        private void MapSuiLogger(LogLevel level, [CanBeNull] string message, [CanBeNull] Exception exception)
        {
            Error(message + ": " + exception?.Message + "\n" + exception?.StackTrace);
        }

        private void UpdateGlobalBoundingBox([NotNull] Rectangle boundingRec, [NotNull] IPosition lsCoordinate)
        {
            if (_boundingBox.IsOutside(new WgsPoint(lsCoordinate.Longitude, lsCoordinate.Latitude))) {
                throw new FlaException("Invalid point");
            }

            if (boundingRec.Top < lsCoordinate.Longitude) {
                boundingRec.Top = lsCoordinate.Longitude;
            }

            if (boundingRec.Bottom > lsCoordinate.Longitude) {
                boundingRec.Bottom = lsCoordinate.Longitude;
            }

            if (boundingRec.Left > lsCoordinate.Latitude) {
                boundingRec.Left = lsCoordinate.Latitude;
            }

            if (boundingRec.Right < lsCoordinate.Latitude) {
                boundingRec.Right = lsCoordinate.Latitude;
            }
        }
    }
}