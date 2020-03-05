using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common;
using Data.Database;
using FutureLoadAnalyzerLib.Tooling.Database;
using JetBrains.Annotations;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using Xunit;
using Xunit.Abstractions;
using Constants = Common.Constants;
using Scenario = Common.Steps.Scenario;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {
    public class XlsxDumperTest : UnitTestBase {
        public XlsxDumperTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunTest()
        {
            PrepareUnitTest();
            RowCollection rc = new RowCollection("mysheet", "MySheet");
            var rb = RowBuilder.Start("t1", "mytxt").Add("blub", 1).Add("blub2", 1);
            rc.Add(rb);
            var rb2 = RowBuilder.Start("t1", "mytxt").Add("blub", 1).Add("blub5", 1);
            rc.Add(rb2);
            XlsxDumper.WriteToXlsx(WorkingDirectory.Combine("t.xlsx"), rc);
        }
    }

    public static class XlsxDumper {
        public static void DumpMultiyearMultiVariableTrendToExcel([NotNull] string fullFileName, [NotNull] MultiyearMultiVariableTrend trend)
        {
            if (trend.Dict.Count == 0) {
                throw new FlaException("No values");
            }

            if (!trend.Dict.ContainsKey(Constants.PresentSlice)) {
                throw new FlaException("Missing present");
            }

            var allVariables = trend.Dict.Values.SelectMany(x => x.Values.Select(y => y.VariableName)).Distinct().ToList();
            using (var p = PrepareExcelPackage(fullFileName)) {
                foreach (var variable in allVariables) {
                    var rcs = MakeRowCollectionForFromMultiTrendVariable(trend, variable, out var categoryCount, out var yearCount);
                    var ws = p.Workbook.Worksheets.Add(variable);
                    int idx = 2;
                    foreach (var rc in rcs) {
                        ws.Cells[idx - 1, 1].Value = rc.SheetName;
                        FillExcelSheet(rc, ws, false, idx);
                        MakeSingleChartForTrends(ws, idx, categoryCount + 4, categoryCount, idx, idx + yearCount, rc.YAxisName, variable);

                        idx = idx + rc.Rows.Count + 10;
                    }
                }

                p.Save();
            }
        }

        public static void DumpMultiyearTrendToExcel([NotNull] string fullFileName, [NotNull] MultiyearTrend trend)
        {
            if (trend.Dict.Count == 0) {
                throw new FlaException("No values");
            }

            if (!trend.Dict.ContainsKey(Constants.PresentSlice)) {
                throw new FlaException("Missing present");
            }

            var allVariables = trend.Dict.Values.SelectMany(x => x.Values.Select(y => y.VariableName)).Distinct().ToList();
            var p = PrepareExcelPackage(fullFileName);


            foreach (var variable in allVariables) {
                var rc = MakeRowCollectionForFromTrendVariable(trend, variable);
                var ws = p.Workbook.Worksheets.Add(rc.SheetName);
                FillExcelSheet(rc, ws, false);
                int columns = trend.Dict.Keys.Select(x => x.DstScenario).Distinct().Count() - 1;
                int rows = trend.Dict.Keys.Select(x => x.DstYear).Distinct().Count() + 1;
                const int chartrow = 4;
                MakeSingleXyChart(ws, chartrow, columns + 1, columns, 2, rows, variable);
            }

            p.Save();
            p.Dispose();
        }

        public static void DumpProfilesToExcel([NotNull] string fullFileName,
                                               int year,
                                               int timeStepWidthInMinutes,
                                               [NotNull] [ItemNotNull] params IWorksheet[] worksheets)
        {
            if (worksheets.Length == 0) {
                throw new FlaException("No worksheets");
            }

            var p = PrepareExcelPackage(fullFileName);
            foreach (var iws in worksheets) {
                RowCollection rc;
                if (iws is IRowCollectionProvider rcp) {
                    rc = rcp.RowCollection;
                    var ws = p.Workbook.Worksheets.Add(rc.SheetName);
                    FillExcelSheet(rc, ws);
                }
                else if (iws is IValueProvider ivp) {
                    var mis = typeof(XlsxDumper).GetMethods();
                    MethodInfo mi = null;
                    foreach (var methodInfo in mis) {
                        if (methodInfo.Name == nameof(MakeRowCollectionForSingleSheet)) {
                            mi = methodInfo;
                        }
                    }

                    if (mi == null) {
                        throw new FlaException();
                    }

                    const int timeColumnOffset = 3;
                    var fooRef = mi.MakeGenericMethod(ivp.ReturnType);
                    List<object> parameters = new List<object>();
                    parameters.Add(year);
                    parameters.Add(timeStepWidthInMinutes);
                    parameters.Add(ivp);
                    rc = (RowCollection)fooRef.Invoke(null, parameters.ToArray());
                    var ws = p.Workbook.Worksheets.Add(rc.SheetName);
                    FillExcelSheet(rc, ws);
                    int startrow = 23 * 24 * 4+2;
                    int endrow = startrow + 7 * 24 * 4;
                    int profilesToInclude = ivp.GetColumnCount();
                    if (ivp.SpecialLineColumnIndex != -1) {
                        profilesToInclude--;
                    }

                    int sumColumn = ivp.GetColumnCount() + 4+timeColumnOffset;
                    for (int columnIdx = 0; columnIdx < ivp.GetColumnCount(); columnIdx++) {
                        int srcCol = columnIdx + timeColumnOffset+1;
                        int targetRow = srcCol; //one down for each col
                        ws.Cells[targetRow, sumColumn].Formula = "=" + ws.Cells[1, srcCol];
                        string colletter = GetExcelColumnName(srcCol);
                        ws.Cells[targetRow, sumColumn + 1].Formula = "=Sum(" + colletter + ":" + colletter + ")/1000000/4";
                    }


                    int endCol = ivp.GetColumnCount() + +timeColumnOffset -1;
                    string endcolltr = GetExcelColumnName(endCol);
                    int tgtCol = ivp.GetColumnCount() + +timeColumnOffset+1;
                    ws.Cells[1, tgtCol].Value = "Netto-Profil";
                    for (int i = 2; i < 35042; i++) {
                        string formula = "=sum(C" + i + ":" + endcolltr + i + ")";
                        ws.Cells[i,tgtCol].Formula = formula;

                    }
                    ws.Cells["B:B"].Style.Numberformat.Format = "dd.mm.yyyy";
                    MakeSingleColumnChart(ws,
                        2,
                        ivp.GetColumnCount() + 3+ timeColumnOffset,
                        profilesToInclude,
                        startrow,
                        endrow,
                        "Winter",
                        "Winter " + ivp.YAxisName,
                        ivp.SpecialLineColumnIndex,
                        96 * 2,
                        ivp.ChartHeight ?? 400,1,2, "B");

                    startrow = (7 + 191 ) * 24 * 4 +2;
                    endrow = startrow + 7 * 24 * 4;
                    MakeSingleColumnChart(ws,
                        27,
                        ivp.GetColumnCount() + 3+timeColumnOffset,
                        profilesToInclude,
                        startrow,
                        endrow,
                        "Sommer",
                        "Sommer " + ivp.YAxisName,
                        ivp.SpecialLineColumnIndex,
                        96 * 2,
                        ivp.ChartHeight ?? 400,1,2, "B");
                    MakeSingleColumnChart(ws,
                        55,
                        ivp.GetColumnCount() + 3+timeColumnOffset,
                        profilesToInclude,
                        2,
                        35040,
                        "Total",
                        ivp.YAxisName,
                        ivp.SpecialLineColumnIndex,
                        96 * 31,
                        ivp.ChartHeight ?? 400,1,0, "C");
                    startrow = 2;
                    endrow = startrow + 7 * 24 * 4;
                    MakeSingleColumnChart(ws,
                        85,
                        ivp.GetColumnCount() + 3+timeColumnOffset,
                        profilesToInclude,
                        startrow,
                        endrow,
                        "Simulation Start",
                        "Simulation Start " + ivp.YAxisName,
                        ivp.SpecialLineColumnIndex,
                        96 * 2,
                        ivp.ChartHeight ?? 400,1,0, "B");
                }
                else {
                    throw new FlaException("");
                }
            }

            p.Save();
            p.Dispose();
        }

        [NotNull]
        public static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = string.Empty;

            while (dividend > 0) {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        [NotNull]
        public static RowCollection MakeRowCollectionForSingleSheet<T>(int year, int timeStepWidthInMinutes, [NotNull] IValueProvider wsc)
        {
            //dump to csv
            RowCollection rc = new RowCollection(wsc.SheetName, wsc.YAxisName);
            DateTime dt = new DateTime(year, 1, 1);
            int columnCount = wsc.GetColumnCount();
            List<ReadOnlyCollection<T>> columns = new List<ReadOnlyCollection<T>>();
            for (int i = 0; i < columnCount; i++) {
                columns.Add(wsc.GetValues<T>(i));
            }

            int maxProfileCount = columns.Max(x => x.Count);
            var names = wsc.GetColumnNames();
            if (names.Count != names.Distinct().Count()) {
                string s = "";
                foreach (var name in names) {
                    var count = names.Count(x => x == name);
                    if (count > 1) {
                        s += name + ": " + count + "\n";
                    }
                }

                throw new FlaException("Profile names need to be unique:" + s);
            }

            if (wsc.GetColumnCount() == 0) {
                throw new FlaException("no profiles");
            }

            foreach (var profile in columns) {
                if (profile.Count == 0) {
                    throw new FlaException("no values in profile");
                }
            }

            for (int row = 0; row < maxProfileCount; row++) {
                RowBuilder rb = RowBuilder.Start("Idx", row);
                rc.Add(rb);
                rb.Add("Time", dt.ToString("ddd, dd.MM.yyyy") + "\n" + dt.ToString("HH:mm"));
                if(row> 5) {
                    rb.Add("Time (date only)", dt.ToString("dd.MM.yyyy"));
                }
                else {
                    rb.Add("Time (date only)", null);
                }

                dt = dt.AddMinutes(timeStepWidthInMinutes);
                for (int col = 0; col < columnCount; col++) {
                    if (columns[col].Count > row) {
                        rb.Add(names[col], columns[col][row]);
                    }
                }
            }

            return rc;
        }

        public static void WriteToXlsx([NotNull] string fileName, [NotNull] [ItemNotNull] List<RowCollection> rowCollections)
        {
            var p = PrepareExcelPackage(fileName);

            foreach (var rowCollection in rowCollections) {
                var ws = p.Workbook.Worksheets.Add(rowCollection.SheetName);
                FillExcelSheet(rowCollection, ws);
            }

            p.Save();
            p.Dispose();
        }

        public static void WriteToXlsx([NotNull] string fileName, [NotNull] [ItemNotNull] params RowCollection[] rowCollections)
        {
            var p = PrepareExcelPackage(fileName);
            foreach (var rowCollection in rowCollections) {
                var ws = p.Workbook.Worksheets.Add(rowCollection.SheetName);
                FillExcelSheet(rowCollection, ws);
            }

            p.Save();
            p.Dispose();
        }


        private static void AddSingleLineChart(int lineColumnNumber,
                                               [NotNull] ExcelWorksheet ws,
                                               [NotNull] ExcelChart chart,
                                               int startrow,
                                               int endrow, int lineColumnThickness)
        {
            int colIdx = lineColumnNumber;
            var name = (string)ws.Cells[1, colIdx].Value;
            string col = GetExcelColumnName(colIdx);
            var ls = chart.PlotArea.ChartTypes.Add(eChartType.Line);
            var ser1 = ls.Series.Add(ws.Cells[col + startrow + ":" + col + endrow], ws.Cells["A" + startrow + ":A" + endrow]);
            ser1.Border.Width = lineColumnThickness;
            ser1.Header = name;
        }

        private static void FillExcelSheet([NotNull] RowCollection rc, [NotNull] ExcelWorksheet ws, bool addFilter = true, int rowoffset = 1)
        {
            if (rc.Rows.Count == 0) {
                throw new FlaException("Not a single row to export. This is probably not intended.");
            }

            if (string.IsNullOrWhiteSpace(rc.SheetName)) {
                throw new FlaException("Sheetname was null");
            }

            List<string> keys = rc.Rows.SelectMany(x => x.Values.Select(y => y.Name)).Distinct().ToList();
            Dictionary<string, int> colidxByKey = new Dictionary<string, int>();
            Dictionary<string, int> colsToSum = new Dictionary<string, int>();
            for (int i = 0; i < keys.Count; i++) {
                colidxByKey.Add(keys[i], i + 1);
                ws.Cells[rowoffset, i + 1].Value = keys[i];
                if (rc.ColumnsToSum.Contains(keys[i])) {
                    colsToSum.Add(keys[i], i + 1);
                }
            }

            int rowIdx = 1 + rowoffset;
            foreach (Row row in rc.Rows) {
                foreach (var pair in row.Values) {
                    int col = colidxByKey[pair.Name];
                    ws.Cells[rowIdx, col].Value = pair.Value;
                }

                rowIdx++;
            }

            int colOffSet = colidxByKey.Count + 2;
            rowIdx = 2;
            foreach (var pair in colsToSum) {
                string col = GetExcelColumnName(pair.Value);
                ws.Cells[rowIdx, colOffSet].Value = pair.Key;
                ws.Cells[rowIdx, colOffSet + 1].Formula = "=sum(" + col + ":" + col + ")/" + rc.SumDivisionFactor;
                rowIdx++;
            }

            ws.View.FreezePanes(2, 1);
            string lastCol = GetExcelColumnName(colidxByKey.Count);
            if (addFilter) {
                ws.Cells["A1:" + lastCol + "1"].AutoFilter = true;
            }
        }

        [NotNull]
        [ItemNotNull]
        private static List<RowCollection> MakeRowCollectionForFromMultiTrendVariable([NotNull] MultiyearMultiVariableTrend trend,
                                                                                      [NotNull] string variable,
                                                                                      out int categoryCount,
                                                                                      out int yearCount)
        {
            var scenarios = trend.Dict.Keys.Select(x => x.DstScenario).Distinct().ToList();
            var categories = trend.Dict.Values.SelectMany(x => x.Values).Where(x => x.VariableName == variable).SelectMany(y => y.Values.Keys)
                .Distinct().ToList();
            categoryCount = categories.Count;
            var years = trend.Dict.Keys.Select(x => x.DstYear).Distinct().OrderBy(x => x).ToList();
            yearCount = years.Count;
            List<RowCollection> rows = new List<RowCollection>();
            var presentSliceValues = trend.Dict[Constants.PresentSlice];

            foreach (var scenario in scenarios) {
                if (scenario == Scenario.Present()) {
                    continue;
                }

                RowCollection rc = new RowCollection(scenario.ShortName, scenario.Name);
                rows.Add(rc);
                foreach (var year in years) {
                    var rb = RowBuilder.Start("Year", year);
                    if (year == 2017) {
                        foreach (var category in categories) {
                            object o = null;
                            if (presentSliceValues != null) {
                                o = presentSliceValues.GetSliceValueByName(variable, category);
                            }

                            rb.Add(category, o);
                        }
                    }
                    else {
                        foreach (var category in categories) {
                            var slicevalues = trend.Dict.Values.FirstOrDefault(x => x.Slice.DstYear == year && x.Slice.DstScenario == scenario);
                            object o = null;
                            if (slicevalues != null) {
                                o = slicevalues.GetSliceValueByName(variable, category);
                            }

                            rb.Add(category, o);
                        }
                    }

                    rc.Add(rb);
                }
            }

            return rows;
        }

        [NotNull]
        private static RowCollection MakeRowCollectionForFromTrendVariable([NotNull] MultiyearTrend trend, [NotNull] string variable)
        {
            string shortendName = variable;
            if (shortendName.Length > 30) {
                shortendName = shortendName.Substring(0, 30);
            }

            RowCollection rc = new RowCollection(shortendName, variable);
            //scenarios
            var scenarios = trend.Dict.Keys.Select(x => x.DstScenario.FriendlyName).Distinct().ToList();

            //values
            var years = trend.Dict.Keys.Select(x => x.DstYear).Distinct().OrderBy(x => x).ToList();
            foreach (var year in years) {
                if (year == 2017) {
                    var rb = RowBuilder.Start("Year", 2017);
                    var val = trend.Dict[Constants.PresentSlice].GetSliceValueByName(variable);
                    foreach (var scenario in scenarios) {
                        if (scenario == Scenario.Present().FriendlyName) {
                            continue;
                        }

                        rb.Add(scenario, val);
                    }

                    rc.Add(rb);
                }
                else {
                    var rb = RowBuilder.Start("Year", year);
                    foreach (var scenario in scenarios) {
                        if (scenario == Scenario.Present().FriendlyName) {
                            continue;
                        }

                        var slicevalues = trend.Dict.Values.FirstOrDefault(x => x.Slice.DstYear == year && x.Slice.DstScenario.FriendlyName == scenario);
                        object o = null;
                        if (slicevalues != null) {
                            o = slicevalues.GetSliceValueByName(variable);
                        }

                        rb.Add(scenario, o);
                    }

                    rc.Add(rb);
                }
            }

            return rc;
        }

        private static void MakeSingleChartForTrends([NotNull] ExcelWorksheet ws,
                                                     int chartRow,
                                                     int chartColumnIdx,
                                                     int numberOfColumns,
                                                     int startrow,
                                                     int endrow,
                                                     [NotNull] string chartname,
                                                     [NotNull] string yaxistitle,
                                                     int width = 610)
        {
            ExcelChart chart = ws.Drawings.AddChart(chartname + "chart", eChartType.AreaStacked);
            chart.RoundedCorners = false;
            chart.Border.Width = 0;
            chart.Style = eChartStyle.Style10;
            chart.YAxis.Title.Text = chartname + " " + yaxistitle;
            chart.YAxis.Title.Font.Size = 10;
            chart.XAxis.CrossBetween = eCrossBetween.MidCat;
            chart.YAxis.CrossBetween = eCrossBetween.MidCat;
            chart.VaryColors = true;
            chart.SetPosition(chartRow, 0, chartColumnIdx + 2, 0);
            chart.SetSize(width, 240);
            for (int i = 0; i < numberOfColumns; i++) {
                var name = (string)ws.Cells[startrow, i + 2].Value;
                string col = GetExcelColumnName(i + 2);
                int startvalrow = startrow + 1;
                var ser1 = chart.Series.Add(ws.Cells[col + startvalrow + ":" + col + endrow], ws.Cells["A" + startvalrow + ":A" + endrow]);
                ser1.Header = name;
            }

            chart.Legend.Position = eLegendPosition.Bottom;
            chart.Legend.Font.Size = 10;
        }


        private static void MakeSingleColumnChart([NotNull] ExcelWorksheet ws,
                                                  int chartRow,
                                                  int chartColumnIdx,
                                                  int numberOfProfileColumns,
                                                  int startrow,
                                                  int endrow,
                                                  [NotNull] string chartname,
                                                  [NotNull] string yaxisTitle,
                                                  int lineColumnNumber,
                                                  int majorUnitInterval,
                                                  double chartheight, int lineColumnThickness1, int lineColumnThickness2,
                                                  string xlabelcol)
        {
            var charttype = eChartType.ColumnStacked;
            if (numberOfProfileColumns < 3) {
                charttype = eChartType.Line;
            }

            ExcelChart chart = ws.Drawings.AddChart(chartname + "chart", charttype);
            chart.RoundedCorners = false;
            chart.YAxis.Title.Text = yaxisTitle;
            chart.YAxis.Title.Font.Size = 10;
            chart.XAxis.MinorTickMark = eAxisTickMark.None;
            chart.XAxis.MajorTickMark = eAxisTickMark.None;
            chart.XAxis.TickLabelPosition = eTickLabelPosition.NextTo;
            chart.XAxis.MajorUnit = majorUnitInterval;
            chart.XAxis.Font.Size = 9;
            if (numberOfProfileColumns == 1) {
                chart.Legend.Remove();
            }
            else {
                chart.Legend.Position = eLegendPosition.Bottom;
            }

            chart.SetPosition(chartRow, 0, chartColumnIdx + 2, 0);
            chart.SetSize(610, (int)chartheight);
            if (charttype == eChartType.ColumnStacked) {
                var barChart = (ExcelBarChart)chart.PlotArea.ChartTypes[0];
                barChart.GapWidth = 0;
            }

            for (int i = 0; i < numberOfProfileColumns; i++) {
                var name = (string)ws.Cells[1, i + 4].Value;
                string col = GetExcelColumnName(i + 4);
                var ser1 = chart.Series.Add(ws.Cells[col + startrow + ":" + col + endrow], ws.Cells[xlabelcol+ startrow + ":" + xlabelcol + endrow]);
                ser1.Header = name;
            }

            if (lineColumnNumber > 0) {
                if (lineColumnThickness1 > 0) {
                    AddSingleLineChart(lineColumnNumber + 4, ws, chart, startrow, endrow, lineColumnThickness1);
                }

                if (lineColumnThickness2 > 0) {
                    AddSingleLineChart(lineColumnNumber + 5, ws, chart, startrow, endrow, lineColumnThickness2);
                }
            }
        }

        private static void MakeSingleXyChart([NotNull] ExcelWorksheet ws,
                                              int chartRow,
                                              int chartColumnIdx,
                                              int numberOfValueColumns,
                                              int startrow,
                                              int endrow,
                                              [NotNull] string chartname)
        {
            ExcelChart chart = ws.Drawings.AddChart("chart" + chartname, eChartType.XYScatterLines);
            chart.SetPosition(chartRow, 0, chartColumnIdx + 2, 0);

            chart.RoundedCorners = false;
            chart.Border.Width = 0;
            chart.YAxis.Title.Text = chartname;
            chart.YAxis.Title.Font.Size = 10;
            chart.XAxis.CrossBetween = eCrossBetween.MidCat;
            chart.YAxis.CrossBetween = eCrossBetween.MidCat;
            chart.SetSize(600, 240);

            for (int i = 0; i < numberOfValueColumns; i++) {
                var name = (string)ws.Cells[1, i + 2].Value;
                string col = GetExcelColumnName(i + 2);
                var ser1 = chart.Series.Add(ws.Cells[col + startrow + ":" + col + endrow], ws.Cells["A" + startrow + ":A" + endrow]);
                ser1.Header = name;
            }

            chart.Axis[1].Title.Text = chartname;
            chart.Legend.Position = eLegendPosition.Bottom;
        }


        [NotNull]
        private static ExcelPackage PrepareExcelPackage([NotNull] string fileName)
        {
            if (File.Exists(fileName)) {
                File.Delete(fileName);
                Thread.Sleep(250);
            }

            var p = new ExcelPackage(new FileInfo(fileName));
            return p;
        }
    }
}