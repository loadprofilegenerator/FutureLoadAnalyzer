using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Data.DataModel;
using JetBrains.Annotations;
using Visualizer.OSM;

namespace BurgdorfStatistics.Tooling {
    public class MapTileRepository {
        private const int NumberOfBuckets = 20;
        [NotNull] [ItemNotNull] private readonly List<OsmFeature> _allFeatures;
        [NotNull] [ItemNotNull] private readonly List<MapTile> _allTiles = new List<MapTile>();
        [NotNull]
        public MapTile BoundingBoxAllFeatures { get; private set; }

        [NotNull] [ItemNotNull] private List<Tuple<double, int>> _latdict;
        [NotNull] [ItemNotNull] private List<Tuple<double, int>> _longdict;
        [ItemNotNull][CanBeNull] private MapTile[,] _mapTileArray;

        public MapTileRepository([NotNull] [ItemNotNull] List<OsmFeature> allFeatures)
        {
            _allFeatures = allFeatures;
            BoundingBoxAllFeatures = BoundingBoxAllFeatures = MakeAllFeatureBoundingBox(_allFeatures);
            SplitIntoTiles();
        }

        [NotNull]
        public OsmFeature FindBestDistanceMatch([NotNull] [ItemNotNull] IReadOnlyCollection<WgsPoint> pointsToLookFor, [NotNull] out WgsPoint closestPoint, out double distance)
        {
            if (pointsToLookFor.Count == 0) {
                throw new Exception("Not a single point to look for");
            }

            var relevantTiles = GetRelevantTiles(pointsToLookFor, NeighbourTileMode.IncludeBorderingTiles);
            var distancesToEachObject = new List<OsmFeatureToPointDistance>();
            foreach (var relevantTile in relevantTiles) {
                distancesToEachObject.AddRange(relevantTile.GetDistances(pointsToLookFor));
            }

            if (distancesToEachObject.Count == 0) {
                throw new Exception("Not a single distance was found");
            }

            distancesToEachObject.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            closestPoint = distancesToEachObject[0].WgsPoint;
            distance = distancesToEachObject[0].Distance;
            return distancesToEachObject[0].Feature;
        }

        [NotNull]
        [ItemNotNull]
        public List<OsmFeature> FindDirectlyMatchingFeatures([NotNull] [ItemNotNull] List<WgsPoint> pointsToLookFor)
        {
            var mapTiles = GetRelevantTiles(pointsToLookFor, NeighbourTileMode.DontIncludeBorderingTiles);
            var matchingOsms = new List<OsmFeature>();
            foreach (var map in mapTiles) {
                var success = map.FindDirectlyMatchingPolygon(pointsToLookFor, out var firstmatchingFeature);
                if (success) {
                    matchingOsms.Add(firstmatchingFeature);
                }
            }

            return matchingOsms;
        }

        [NotNull]
        [ItemNotNull]
        private List<MapTile> GetRelevantTiles([NotNull] [ItemNotNull] IReadOnlyCollection<WgsPoint> pointsToLookFor, NeighbourTileMode neighourMode)
        {
            var mapTiles = new List<MapTile>();
            if (_mapTileArray == null) {
                throw new FlaException();
            }
            foreach (var point in pointsToLookFor) {
                var longCoord = -1;
                for (var lonidx = 0; lonidx < _longdict.Count - 1; lonidx++) {
                    if (point.Lon >= _longdict[lonidx].Item1 && point.Lon <= _longdict[lonidx + 1].Item1) {
                        longCoord = lonidx;
                        lonidx = _longdict.Count;
                    }
                }

                var latcoord = -1;
                for (var latidx = 0; latidx < _latdict.Count - 1; latidx++) {
                    if (point.Lat >= _latdict[latidx].Item1 && point.Lat <= _latdict[latidx + 1].Item1) {
                        latcoord = latidx;
                        latidx = _latdict.Count;
                    }
                }


                if (latcoord == -1 || longCoord == -1) {
                    throw new Exception("Couldn't find any tile");
                }

                if (!mapTiles.Contains(_mapTileArray[latcoord, longCoord])) {
                    mapTiles.Add(_mapTileArray[latcoord, longCoord]);
                }

                if (neighourMode == NeighbourTileMode.IncludeBorderingTiles) {
                    for (var xidx = -1; xidx < 2; xidx++) {
                        for (var yidx = -1; yidx < 2; yidx++) {
                            var x1 = latcoord + xidx;
                            var y1 = longCoord + yidx;
                            if (x1 >= 0 && x1 <= _mapTileArray.GetLength(0) && y1 >= 0 && y1 <= _mapTileArray.GetLength(1)) {
                                var mt = _mapTileArray[x1, y1];
                                if (!mapTiles.Contains(mt)) {
                                    mapTiles.Add(mt);
                                }
                            }
                        }
                    }
                }
            }

            return mapTiles;
        }

        [NotNull]
        private MapTile MakeAllFeatureBoundingBox([NotNull] [ItemNotNull] List<OsmFeature> osmFeatures)
        {
            var boundingBox = new MapTile(double.MaxValue, double.MinValue, double.MaxValue, double.MinValue);
            foreach (var feature in osmFeatures) {
                foreach (var wgsPoint in feature.WgsPoints) {
                    if (wgsPoint.Lat < boundingBox.LeftLat) {
                        boundingBox.LeftLat = wgsPoint.Lat;
                    }

                    if (wgsPoint.Lat > boundingBox.RightLat) {
                        boundingBox.RightLat = wgsPoint.Lat;
                    }

                    if (wgsPoint.Lon < boundingBox.BottomLon) {
                        boundingBox.BottomLon = wgsPoint.Lon;
                    }

                    if (wgsPoint.Lon > boundingBox.TopLon) {
                        boundingBox.TopLon = wgsPoint.Lon;
                    }
                }
            }

            return boundingBox;
        }

        private void SplitIntoTiles()
        {
            _latdict = new List<Tuple<double, int>>();
            _longdict = new List<Tuple<double, int>>();
            var latInterval = (BoundingBoxAllFeatures.RightLat - BoundingBoxAllFeatures.LeftLat) / (NumberOfBuckets - 1);
            if (latInterval < 0) {
                throw new Exception("Inverted box left right");
            }

            var lonInterval = (BoundingBoxAllFeatures.TopLon - BoundingBoxAllFeatures.BottomLon) / (NumberOfBuckets - 1);
            if (lonInterval < 0) {
                throw new Exception("Inverted box top bottom");
            }

            var currLat = BoundingBoxAllFeatures.LeftLat;
            var fullList = new MapTile[NumberOfBuckets, NumberOfBuckets];
            double currLon;
            for (var latBucket = 0; latBucket < NumberOfBuckets; latBucket++) {
                _latdict.Add(new Tuple<double, int>(currLat, latBucket));
                currLon = BoundingBoxAllFeatures.BottomLon;
                for (var lonBucket = 0; lonBucket < NumberOfBuckets; lonBucket++) {
                    var r = new MapTile(currLat, currLon + lonInterval, currLon, currLat + latInterval);
                    fullList[latBucket, lonBucket] = r;
                    _allTiles.Add(r);
                    currLon += lonInterval;
                }

                currLat += latInterval;
            }

            currLon = BoundingBoxAllFeatures.BottomLon;
            for (var ybucket = 0; ybucket < NumberOfBuckets; ybucket++) {
                _longdict.Add(new Tuple<double, int>(currLon, ybucket));
                currLon += lonInterval;
            }

            // splitterTest
            //TestSplitting(allTiles);


            if (_longdict.Count != NumberOfBuckets) {
                throw new Exception("Logic error");
            }

            if (_latdict.Count != NumberOfBuckets) {
                throw new Exception("Logic error");
            }

            foreach (var feature in _allFeatures) {
                foreach (var point in feature.WgsPoints) {
                    if (!BoundingBoxAllFeatures.IsInside(point)) {
                        throw new Exception("Point is not in global bounding box");
                    }

                    var foundAtLeastOneTile = false;
                    foreach (var tile in _allTiles) {
                        if (tile.IsInside(point)) {
                            foundAtLeastOneTile = true;
                            if (!tile.OsmFeaturesInRectangle.Contains(feature)) {
                                tile.OsmFeaturesInRectangle.Add(feature);
                            }
                        }
                    }

                    if (!foundAtLeastOneTile) {
                        throw new Exception("No tile found");
                    }
                }
            }

            var totalAssignedFeatures = _allTiles.Select(x => x.OsmFeaturesInRectangle.Count).Sum();
            if (totalAssignedFeatures == 0) {
                throw new Exception("no features were assigned");
            }

            /* if(_allFeatures.Count != totalAssignedFeatures)
             {
                 throw new Exception("not all features assigned");
             }*/
            _mapTileArray = fullList;
        }

        /*
        private void TestSplitting(List<MapTile> allTiles)
        {
            int successCount = 0;
            int latcount = 0;

            for (double currLat = BoundingBoxAllFeatures.LeftLat; currLat < BoundingBoxAllFeatures.RightLat; currLat += 0.0001)
            {
                int longcount = 0;
                for (double currLon = BoundingBoxAllFeatures.BottomLon; currLon < BoundingBoxAllFeatures.TopLon; currLon += 0.0001)
                {
                    bool foundAny = false;
                    WGSPoint wp = new WGSPoint(currLon, currLat);
                    foreach (MapTile allTile in allTiles)
                    {
                        if (allTile.IsInside(wp))
                        {
                            foundAny = true;
                            successCount++;
                            break;
                        }

                    }

                    if (!foundAny)
                    {
                        throw new Exception("didn't find a tile for these coordinates");
                    }

                    longcount++;
                }

                latcount++;
            }
        }*/
    }
}