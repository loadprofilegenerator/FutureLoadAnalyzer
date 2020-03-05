using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data;
using Data.DataModel;
using JetBrains.Annotations;
using Svg;
using Visualizer.OSM;

namespace Visualizer.Mapper {
    public class MapDrawer : BasicLoggable {
        public MapDrawer([NotNull] ILogger logger, Stage myStage) : base(logger, myStage, nameof(MapDrawer))
        {
        }

        public void DrawMapSvg([ItemCanBeNull] [NotNull] List<MapPoint> wgsmapPointsWithNull,
                               [NotNull] string fileName,
                               [ItemNotNull] [NotNull] List<MapLegendEntry> plegendEntries)
        {
            var legendEntries = plegendEntries.ToList();
            if (wgsmapPointsWithNull.Count == 0) {
                throw new FlaException("not a single mappoint was given");
            }

            var mapPointTypes = wgsmapPointsWithNull.Where(x => x != null).Select(x => x.Mode).Distinct().ToList();
            if (mapPointTypes.Count != 1) {
                throw new FlaException("different types of map points.");
            }

            var mapPoints = wgsmapPointsWithNull.Where(x => x != null).Select(x => x.ConvertToSwissMapPoint()).ToList();
            var xmin = mapPoints.Min(x => x.X);
            var ymin = mapPoints.Min(x => x.Y);
            var xmax = mapPoints.Max(x => x.X);
            var ymax = mapPoints.Max(x => x.Y);

            // ReSharper disable twice PossibleInvalidOperationException
            const int coordfactor = 1;
            var width = (int)((xmax.Value - xmin.Value) * coordfactor);
            // ReSharper disable twice PossibleInvalidOperationException
            var height = (int)((ymax.Value - ymin.Value) * coordfactor);
            var min = mapPoints.Min(x => x.Value);
            var max = mapPoints.Max(x => x.Value);
            var spread = max - min;
            if (spread < 1) {
                spread = 1;
            }

            var factor = 255 / spread;


            var doc = new SvgDocument {
                Width = width,
                Height = height,
                ViewBox = new SvgViewBox(0, 0, width, height)
            };

            var group = new SvgGroup();
            doc.Children.Add(group);
            var legendY = 100;
            foreach (var entry in legendEntries) {
                SvgText c = new SvgText(entry.Name) {
                    Color = new SvgColourServer(Color.FromArgb(entry.R, entry.G, entry.B)),
                    FontSize = 100,
                    Fill = new SvgColourServer(Color.FromArgb(entry.R, entry.G, entry.B))
                };
                var svgux = new SvgUnitCollection {
                    new SvgUnit(100)
                };
                c.X = svgux;
                var svguy = new SvgUnitCollection {
                    new SvgUnit(legendY)
                };
                c.Y = svguy;
                c.Y = svguy;
                legendY += 100;
                group.Children.Add(c);
            }

            var messedUpPoints = 0;
            foreach (var mapPoint in mapPoints) {
                if (mapPoint.X == null) {
                    Error("Mappoint with x == null #" + messedUpPoints);
                    messedUpPoints++;
                    continue;
                }

                if (mapPoint.Y == null) {
                    Error("Mappoint with y == null #" + messedUpPoints);
                    messedUpPoints++;
                    continue;
                }

                var c = new SvgCircle {
                    CenterX = mapPoint.AdjustedX(xmin, width, coordfactor, false),
                    CenterY = mapPoint.AdjustedY(ymin, height, coordfactor),
                    Radius = mapPoint.Radius
                };
                var red = (mapPoint.Value - min) * factor;
                Color col;
                if (mapPoint.Mode == MapPointMode.DotAbsoluteColor) {
                    col = Color.FromArgb(255, mapPoint.R, mapPoint.G, mapPoint.B);
                }
                else {
                    col = Color.FromArgb(255, (byte)red, 0, 0);
                }

                c.Fill = new SvgColourServer(col);
                c.Stroke = new SvgColourServer(col);
                c.StrokeWidth = 1;
                group.Children.Add(c);
            }

            using (var stream = new FileStream(fileName, FileMode.Create)) {
                Debug("Wrote svg map to " + fileName);
                doc.Write(stream);
            }

            //var bitmap = doc.Draw();
            //string pngfileName = fileName.Replace(".svg", ".png");
            //bitmap.Save(pngfileName, ImageFormat.Png);
        }

        /* public void DrawMapPng([ItemNotNull] [NotNull] List<MapPoint> mapPoints, [NotNull] string fileName )
         {
             double? xmin = mapPoints.Min(x => x.X) ;
             double? ymin = mapPoints.Min(x => x.Y);
             double? xmax = mapPoints.Max(x => x.X);
             double? ymax = mapPoints.Max(x => x.Y);
             int maxRadius = mapPoints.Max(x => x.Radius);
             // ReSharper disable twice PossibleInvalidOperationException
             int width =(int) (xmax.Value - xmin.Value)+200+maxRadius;
             // ReSharper disable twice PossibleInvalidOperationException
             int height = (int)(ymax.Value - ymin.Value)+200+maxRadius;
             double min = mapPoints.Min(x => x.Value)*0.9;
             double max = mapPoints.Max(x => x.Value);
             double spread = max - min;
             if (spread < 1) {
                 spread = 1;
             }

             double factor = 255 / spread;

             using (var bmp = new BitmapPreparer(width,height)) {

                 foreach (MapPoint mapPoint in mapPoints) {
                     double red = (mapPoint.Value - min) * factor;
                     System.Windows.Media.Color c = System.Windows.Media.Color.FromRgb((byte)red, 0, 0);
                     int xpos = mapPoint.AdjustedX(xmin,width);
                     int ypos = mapPoint.AdjustedY(ymin,height);
                     for (int x = xpos - mapPoint.Radius; x < xpos + mapPoint.Radius && x < width; x++) {
                         for (int y = ypos - mapPoint.Radius; y < ypos + mapPoint.Radius && y < height; y++) {
                             bmp.SetPixel(x, y, c);
                         }
                     }
                 }

                 Log("Saving the carpet plot for " + fileName + "...");
                 using (
                     var fs = new FileStream(fileName, FileMode.Create))  {
                     var encoder = new PngBitmapEncoder();
                     encoder.Frames.Add(BitmapFrame.Create(bmp.GetBitmap()));
                     encoder.Save(fs);
                 }
             }
         }*/
    }
}