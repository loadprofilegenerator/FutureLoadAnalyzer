using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data;
using Data.DataModel;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Visualizer.Mapper;
using Visualizer.OSM;
using Visualizer.Sankey;
using ArrowAnnotation = OxyPlot.Annotations.ArrowAnnotation;
using CategoryAxis = OxyPlot.Axes.CategoryAxis;
using ColumnSeries = OxyPlot.Series.ColumnSeries;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using SvgExporter = OxyPlot.SvgExporter;

namespace Visualizer {
    public enum ExportType {
        Png,
        SVG
    }

    public class PlotMaker : BasicLoggable {
        [NotNull] private readonly MapDrawer _mapDrawer;

        [ItemNotNull] [NotNull] private readonly List<ThreadClass> _myThreads = new List<ThreadClass>();

        public PlotMaker([NotNull] MapDrawer mapDrawer, [NotNull] ILogger logger, [CanBeNull] OsmMapDrawer osmMapDrawer) : base(logger,
            Stage.Plotting,
            nameof(PlotMaker))
        {
            OsmMapDrawer = osmMapDrawer;
            _mapDrawer = mapDrawer;
        }

        [CanBeNull]
        private OsmMapDrawer OsmMapDrawer { get; }

        public void AddThread([NotNull] string name, [NotNull] Action func, [NotNull] string dstFullFileName)
        {
            var tc = new ThreadClass(name, func, dstFullFileName, MyLogger);
            lock (_myThreads) {
                _myThreads.Add(tc);
            }

            WaitforThreadsToFinish(10);
        }

        public void Finish()
        {
            List<string> files;
            lock (_myThreads) {
                files = _myThreads.Select(x => x.DstFullFileName).ToList();
            }

            if (files.Count != files.Distinct().Count()) {
                string s = "";
                foreach (var filename in files) {
                    var count = files.Count(x => x == filename);
                    if (count > 1) {
                        Info(filename + ": " + count);
                        s += filename + ":" + count + "\n";
                    }
                }

                throw new FlaException("at least one file is registered twice: " + s);
            }

            Thread.Sleep(500);
            WaitforThreadsToFinish(0);
        }

        public void MakeBarChart([NotNull] string filename,
                                 [NotNull] string comparisonName,
                                 [NotNull] [ItemNotNull] List<BarSeriesEntry> barseries,
                                 [NotNull] [ItemNotNull] List<string> categorylabels,
                                 ExportType exportType = ExportType.Png)
        {
            if (barseries.Count == 0) {
                throw new FlaException("Trying to make a chart of an empty series");
            }

            AddThread(comparisonName, () => InternalMakeBarChart(filename, comparisonName, barseries, categorylabels, exportType), filename);
        }

        public void MakeLineChart([NotNull] string filename,
                                  [NotNull] string yaxislabel,
                                  [NotNull] LineSeriesEntry lineSeries,
                                  [NotNull] [ItemNotNull] List<AnnotationEntry> annotations,
                                  double absoluteMinimum = 0,
                                  ExportType exportType = ExportType.Png)
        {
            List<LineSeriesEntry> lineseriesList = new List<LineSeriesEntry> {lineSeries};
            AddThread(yaxislabel,
                () => InternalMakeLineChart(filename, yaxislabel, lineseriesList, annotations, absoluteMinimum, exportType),
                filename);
        }


        public void MakeLineChart([NotNull] string filename,
                                  [NotNull] string yaxislabel,
                                  [NotNull] [ItemNotNull] List<LineSeriesEntry> lineSeries,
                                  [NotNull] [ItemNotNull] List<AnnotationEntry> annotations,
                                  double absoluteMinimum = 0,
                                  ExportType exportType = ExportType.Png)
        {
            AddThread(yaxislabel, () => InternalMakeLineChart(filename, yaxislabel, lineSeries, annotations, absoluteMinimum, exportType), filename);
        }

        public void MakeLineChart([NotNull] string filename, [NotNull] string yaxislabel, [NotNull] Profile p, ExportType exportType = ExportType.Png)
        {
            var slineSeries = p.GetLineSeriesEntry();
            var lineSeries = new List<LineSeriesEntry> {
                slineSeries
            };
            var annotations = new List<AnnotationEntry>();
            const double absoluteMinimum = 0;
            AddThread(yaxislabel, () => InternalMakeLineChart(filename, yaxislabel, lineSeries, annotations, absoluteMinimum, exportType), filename);
        }

        public void MakeMapDrawer([NotNull] string filename,
                                  [NotNull] string threadname,
                                  [NotNull] [ItemNotNull] List<MapPoint> points,
                                  [ItemNotNull] [NotNull] List<MapLegendEntry> legendEntries)
        {
            if (string.IsNullOrWhiteSpace(threadname)) {
                throw new FlaException("name was empty");
            }

            AddThread(threadname, () => _mapDrawer.DrawMapSvg(points, filename, legendEntries), filename);
        }

        public void MakeOsmMap([NotNull] string mapname,
                               [NotNull] string dstFileName,
                               [NotNull] [ItemNotNull] List<MapColorEntryWithOsmGuid> colorsWithOsmGuids,
                               [NotNull] [ItemNotNull] List<WgsPoint> additionalPoints,
                               [NotNull] [ItemNotNull] List<MapLegendEntry> mapLabels,
                               [NotNull] [ItemNotNull] List<LineEntry> lines)
        {
            if (OsmMapDrawer == null) {
                throw new FlaException("No mapdrawer");
            }

            AddThread(mapname, () => OsmMapDrawer.MakeMap(dstFileName, colorsWithOsmGuids, additionalPoints, mapLabels, lines), dstFileName);
        }

        public void MakeOsmMap([NotNull] string mapname,
                               [NotNull] string dstFileName,
                               [NotNull] [ItemNotNull] List<MapColorEntryWithHouseGuid> colorsWithHouseGuids,
                               [NotNull] [ItemNotNull] List<WgsPoint> additionalPoints,
                               [NotNull] [ItemNotNull] List<MapLegendEntry> mapLabels,
                               [NotNull] [ItemNotNull] List<LineEntry> lines)
        {
            if (OsmMapDrawer == null) {
                throw new FlaException("No mapdrawer");
            }

            AddThread(mapname, () => OsmMapDrawer.MakeMap(dstFileName, colorsWithHouseGuids, additionalPoints, mapLabels, lines), dstFileName);
        }

        public void MakeSankeyChart([NotNull] SingleSankeyArrow arrow)
        {
            var helper = new ZZ_Sankeyhelper(MyLogger, arrow.MyStage);
            helper.Run(arrow);
        }

        public void MakeSankeyChart([NotNull] [ItemNotNull] List<SingleSankeyArrow> arrow)
        {
            var helper = new ZZ_Sankeyhelper(MyLogger, arrow.First().MyStage);
            AddThread(arrow[0].ArrowName, () => helper.Run(arrow), arrow[0].FullPngFileName());
        }

        [NotNull]
        private static PlotModel CreatePlottingCanvas()
        {
            var plotModel = new PlotModel {
                Title = "",
                LegendBorderThickness = 0,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                DefaultFont = "Calibri",
                DefaultFontSize = 24,
                PlotAreaBorderColor = OxyColors.Transparent
            };
            return plotModel;
        }

        private static void Export([NotNull] PlotModel pm, [NotNull] string fullFileName, ExportType exportType)
        {
            if (exportType == ExportType.Png) {
                var pngExporter = new PngExporter {Width = 1600, Height = 1000, Background = OxyColors.White};
                pngExporter.ExportToFile(pm, fullFileName);
            }

            if (exportType == ExportType.SVG) {
                var exporter = new SvgExporter {Width = 1600, Height = 1000};
                using (var stream = File.Create(fullFileName.Replace(".png", ".1.svg"))) {
                    exporter.Export(pm, stream);
                }
            }
        }

        private void InternalMakeBarChart([NotNull] string filename,
                                          [NotNull] string comparisonName,
                                          [NotNull] [ItemNotNull] List<BarSeriesEntry> barseries,
                                          [NotNull] [ItemNotNull] List<string> categorylabels,
                                          ExportType exportType)
        {
            if (exportType == ExportType.Png && barseries.Max(x => x.Values.Count) > 1000) {
                throw new FlaException("export over 1000 values for bar charts only  as svg due to performance");
            }

            try {
                var pm = CreatePlottingCanvas();
                var min = barseries.Select(x => x.Values.Min()).Min();
                var yasismin = Math.Min(min, 0);
                MakeCategoryXAxis(pm, categorylabels);
                MakeYAxis(pm, comparisonName, yasismin);

                foreach (var barSeriesEntry in barseries) {
                    var cs = new ColumnSeries {
                        Title = barSeriesEntry.Name,
                        IsStacked = true,
                        StrokeThickness = 0.0
                    };
                    for (var index = 0; index < barSeriesEntry.Values.Count; index++) {
                        var value = barSeriesEntry.Values[index];
                        cs.Items.Add(new ColumnItem(value));
                    }

                    pm.Series.Add(cs);
                }

                Export(pm, filename, exportType);
            }
            catch (Exception ex) {
                Error("Exception while making a chart: " + ex.Message);
                throw;
            }
        }

        private static void InternalMakeLineChart([NotNull] string filename,
                                                  [NotNull] string comparisonName,
                                                  [NotNull] [ItemNotNull] List<LineSeriesEntry> lineSeries,
                                                  [NotNull] [ItemNotNull] List<AnnotationEntry> annotations,
                                                  double absoluteMinimum,
                                                  ExportType exportType)
        {
            var pm = CreatePlottingCanvas();
            MakeYAxis(pm, comparisonName, absoluteMinimum);
            MakeXAxis(pm, "");

            foreach (var lineSeriesEntry in lineSeries) {
                var cs = new LineSeries {
                    Title = lineSeriesEntry.Name
                };
                foreach (var value in lineSeriesEntry.Values) {
                    cs.Points.Add(new DataPoint(value.X, value.Y));
                }

                pm.Series.Add(cs);
            }

            foreach (var a in annotations) {
                var arrowAnnotation1 = new ArrowAnnotation {
                    Color = OxyColors.Green,
                    EndPoint = new DataPoint(a.X1, a.Y1),
                    Text = a.Text,
                    TextVerticalAlignment = VerticalAlignment.Bottom,
                    ArrowDirection = new ScreenVector(a.Direction * 30, a.YOffset * -1)
                };
                pm.Annotations.Add(arrowAnnotation1);
            }

            Export(pm, filename, exportType);
        }

        private static void MakeCategoryXAxis([NotNull] PlotModel plotModel, [NotNull] [ItemNotNull] List<string> categorylabels)
        {
            var xAxis = new CategoryAxis {
                Title = "",
                Position = AxisPosition.Bottom
            };
            xAxis.Labels.AddRange(categorylabels);
            xAxis.MajorGridlineStyle = LineStyle.None;
            xAxis.AxislineColor = OxyColors.Black;
            xAxis.AxislineStyle = LineStyle.Solid;
            xAxis.MinimumPadding = 0;
            xAxis.GapWidth = 0.0;
            xAxis.Angle = -90;
            if (categorylabels.Count > 100 || categorylabels.Count == 0) {
                xAxis.MajorStep = 60;
            }
            else {
                xAxis.MajorStep = 1;
            }

            plotModel.Axes.Add(xAxis);
        }

        private static void MakeXAxis([NotNull] PlotModel pm, [NotNull] string title)
        {
            var yAxis = new LinearAxis {
                AbsoluteMinimum = 0,
                MaximumPadding = 0.06,
                //MinimumPadding = 0,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                //MajorStep = xAxisLabels.Count/5.0,
                MinorGridlineStyle = LineStyle.None,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColors.Black,
                //MinorStep = 5,
                Title = title,
                //PositionAtZeroCrossing = true,
                Position = AxisPosition.Bottom
            };
            pm.Axes.Add(yAxis);
        }

        private static void MakeYAxis([NotNull] PlotModel pm, [NotNull] string title, double absoluteMinimum)
        {
            var yAxis = new LinearAxis {
                AbsoluteMinimum = absoluteMinimum,
                MaximumPadding = 0.06,
                //MinimumPadding = 0,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                //MajorStep = xAxisLabels.Count/5.0,
                MinorGridlineStyle = LineStyle.None,
                AxislineStyle = LineStyle.Solid,
                AxislineColor = OxyColors.Black,
                //MinorStep = 5,
                Title = title,
                //PositionAtZeroCrossing = true,
                Position = AxisPosition.Left
            };

            pm.Axes.Add(yAxis);
        }

        private void WaitforThreadsToFinish(int threadlimit)
        {
            int threadcount;
            lock (_myThreads) {
                threadcount = _myThreads.Count;
            }

            if (threadcount < threadlimit) {
                return;
            }

            while (threadcount > 0) {
                ThreadClass t = null;
                lock (_myThreads) {
                    if (_myThreads.Count > 0) {
                        t = _myThreads[0];
                    }
                }

                if (t != null) {
                    t.Thread.Join();

                    if (t.Ex != null) {
                        Info(t.Ex.StackTrace);
                        throw t.Ex;
                    }

                    if (!File.Exists(t.DstFullFileName)) {
                        throw new FlaException("Missing result file: " + t.DstFullFileName);
                    }
                }

                lock (_myThreads) {
                    _myThreads.Remove(t);
                    threadcount = _myThreads.Count;
                }
            }
        }

        private class ThreadClass : BasicLoggable {
            public ThreadClass([NotNull] string name, [NotNull] Action func, [NotNull] string dstFullFileName, [NotNull] ILogger logger) : base(
                logger,
                Stage.Plotting,
                name)
            {
                if (File.Exists(dstFullFileName)) {
                    File.Delete(dstFullFileName);
                }

                Func = func;
                var t = ThreadProvider.Get().MakeThreadAndStart(SaveExecute, name, true);
                Thread = t;
                DstFullFileName = dstFullFileName;
            }

            [NotNull]
            public string DstFullFileName { get; }

            [CanBeNull]
            public Exception Ex { get; set; }

            [NotNull]
            public Action Func { get; }


            [NotNull]
            public Thread Thread { get; }

            public void SaveExecute()
            {
                try {
                    var sw = new Stopwatch();
                    sw.Start();
                    Func();
                    sw.Stop();
                    Info("Executing " + Name + " took " + sw.ElapsedMilliseconds + " ms");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Ex = ex;
                }
            }
        }
    }
}