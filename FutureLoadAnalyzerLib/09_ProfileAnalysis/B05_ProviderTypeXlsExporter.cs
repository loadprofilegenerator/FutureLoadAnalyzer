using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B05_ProviderTypeXlsExporter : RunableForSingleSliceWithBenchmark {
        public B05_ProviderTypeXlsExporter([NotNull] ServiceRepository services) : base(nameof(B05_ProviderTypeXlsExporter),
            Stage.ProfileAnalysis,
            205,
            services,
            false)
        {
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var fnX = MakeAndRegisterFullFilename(FilenameHelpers.CleanFileName("ExportForAllProviderTypesXlsDumper") + ".xlsx", slice);
            ProcessOneSumTypeWithXls(slice, fnX, SumType.ByProvider);
            var fn = MakeAndRegisterFullFilename(FilenameHelpers.CleanFileName("ExportForAllProviderTypes") + ".xlsx", slice);
            ProcessOneSumType(slice, fn, SumType.ByProvider);
            var fn2 = MakeAndRegisterFullFilename(FilenameHelpers.CleanFileName("ExportForAllProfileSources") + ".xlsx", slice);
            ProcessOneSumType(slice, fn2, SumType.ByProfileSource);
        }

        private void AddBkwChart([NotNull] [ItemNotNull] List<ArchiveEntry> providerentries,
                                 [NotNull] ExcelWorksheet ws,
                                 [NotNull] ExcelChart chart,
                                 int startrow,
                                 int endrow)
        {
            int colIdx = providerentries.Count + 2;
            var name = (string)ws.Cells[1, colIdx].Value;
            string col = XlsxDumper.GetExcelColumnName(colIdx);
            Trace("column " + col);
            var ls = chart.PlotArea.ChartTypes.Add(eChartType.Line);
            var ser1 = ls.Series.Add(ws.Cells[col + startrow + ":" + col + endrow], ws.Cells["A" + startrow + ":A" + endrow]);
            ser1.Header = name;
        }

        private void MakeSingleChart([NotNull] ExcelWorksheet ws,
                                     int chartRow,
                                     int columnIdx,
                                     [NotNull] [ItemNotNull] List<ArchiveEntry> providerentries,
                                     int startrow,
                                     int endrow,
                                     [NotNull] string chartname,
                                     int width = 800)
        {
            ExcelChart chart = ws.Drawings.AddChart(chartname + "chart", eChartType.ColumnStacked);
            chart.Title.Text = chartname;
            chart.SetPosition(chartRow, 0, columnIdx + 2, 0);
            chart.SetSize(width, 500);
            var barChart = (ExcelBarChart)chart.PlotArea.ChartTypes[0];
            barChart.GapWidth = 0;

            for (int i = 0; i < providerentries.Count; i++) {
                var name = (string)ws.Cells[1, i + 2].Value;
                string col = XlsxDumper.GetExcelColumnName(i + 2);
                Trace("column " + col);
                var ser1 = chart.Series.Add(ws.Cells[col + startrow + ":" + col + endrow], ws.Cells["A" + startrow + ":A" + endrow]);
                ser1.Header = name;
            }

            chart.Legend.Position = eLegendPosition.Bottom;

            AddBkwChart(providerentries, ws, chart, startrow, endrow);
        }
        private void ProcessOneSumTypeWithXls([NotNull] ScenarioSliceParameters slice, [NotNull] string fn, SumType sumType)
        {
            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
            var entries = saHouses.LoadAllOrMatching();
            var providerentries = entries.Where(x => x.Key.SumType == sumType).ToList();
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var bkwJ = dbRaw.Fetch<BkwProfile>();
            foreach (var ae in providerentries) {
                if (sumType == SumType.ByProvider) {
                    ae.Profile.Name = ae.Key.ProviderType + " " + ae.GenerationOrLoad;
                }
                else if (sumType == SumType.ByProfileSource) {
                    ae.Profile.Name = ae.Key.ProfileSource ?? throw new InvalidOperationException();
                }
                else {
                    throw new FlaException("Unknown sum type");
                }
            }
            var profiles = providerentries.Select(x => x.Profile).ToList();
            var bkw = new Profile(bkwJ[0].Profile);
            bkw.Name = "Messung 2017";
            profiles.Add(bkw);
            XlsxDumper.DumpProfilesToExcel(fn,2017,15,new ProfileWorksheetContent("Profile","Leistung [MW]",bkw.Name,profiles));
        }
        private void ProcessOneSumType([NotNull] ScenarioSliceParameters slice, [NotNull] string fn, SumType sumType)
        {
            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
            int columnIdx = 2;

            using (ExcelPackage p = new ExcelPackage()) {
                var entries = saHouses.LoadAllOrMatching();
                var providerentries = entries.Where(x => x.Key.SumType == sumType).ToList();
                var ws = p.Workbook.Worksheets.Add("sheet1");
                int sumCol = providerentries.Count + 4;
                foreach (var entry in providerentries) {
                    string colLabel;
                    if (sumType == SumType.ByProvider) {
                        colLabel = entry.Key.ProviderType;
                    }
                    else if (sumType == SumType.ByProfileSource) {
                        colLabel = entry.Key.ProfileSource;
                    }
                    else {
                        throw new FlaException("Unknown sum type");
                    }

                    ws.Cells[1, columnIdx].Value = colLabel;
                    int rowIdx = 2;
                    var vals = entry.Profile.ConvertFromEnergyToPower();
                    double multiplier = 1;
                    if (entry.GenerationOrLoad == GenerationOrLoad.Generation) {
                        multiplier = -1;
                    }

                    for (int i = 0; i < vals.Values.Count; i++) {
                        ws.Cells[rowIdx, columnIdx].Value = vals.Values[i] * multiplier;
                        rowIdx++;
                    }

                    ws.Cells[columnIdx, sumCol].Value = colLabel;
                    string colletter = XlsxDumper.GetExcelColumnName(columnIdx);
                    ws.Cells[columnIdx, sumCol + 1].Formula = "=Sum(" + colletter + ":" + colletter + ")/1000000/4";

                    columnIdx++;
                }

                WriteBkwColumn(ws, columnIdx, sumCol);

                //winter chart
                int startrow = 2;
                int endrow = startrow + 24 * 4 * 14;
                int chartRow = columnIdx + 2;
                MakeSingleChart(ws, chartRow, columnIdx, providerentries, startrow, endrow, "Winter");

                //sommer chart
                startrow = 24 * 4 * (7 + 130);
                endrow = startrow + 24 * 4 * 7;
                chartRow += 30;

                MakeSingleChart(ws, chartRow, columnIdx, providerentries, startrow, endrow, "Sommer");

                //sommer chart
                startrow = 10000;
                endrow = 11000;
                chartRow += 30;

                MakeSingleChart(ws, chartRow, columnIdx, providerentries, startrow, endrow, "Frühjahr");
                p.SaveAs(new FileInfo(fn));
                SaveToArchiveDirectory(fn, RelativeDirectory.Report, slice);
            }

            Info("saved " + fn);
        }

        private void WriteBkwColumn([NotNull] ExcelWorksheet ws, int columnIdx, int sumCol)
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var bkw = dbRaw.Fetch<BkwProfile>();
            ws.Cells[1, columnIdx].Value = "BKW";
            int rowIdx = 2;
            var vals = bkw[0].Profile.Values;
            for (int i = 0; i < vals.Count; i++) {
                ws.Cells[rowIdx, columnIdx].Value = vals[i];
                rowIdx++;
            }

            ws.Cells[columnIdx, sumCol].Value = "BKW";
            string colletter = XlsxDumper.GetExcelColumnName(columnIdx);
            ws.Cells[columnIdx, sumCol + 1].Formula = "=Sum(" + colletter + ":" + colletter + ")/1000000/4";
        }
    }
}