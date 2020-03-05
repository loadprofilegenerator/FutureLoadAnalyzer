using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace Common.Config {
    public class ScenarioSheetHandler : BasicLoggable {
        public ScenarioSheetHandler([NotNull] ILogger logger) : base(logger, Stage.Preparation, nameof(ScenarioSheetHandler))
        {
        }

        public static void FillMissingColumns([NotNull] ExcelWorksheet ws, [NotNull] Dictionary<string, int> rowDict)
        {
            List<int> years = new List<int> {
                2017, 2020, 2025, 2030, 2035, 2040, 2045, 2050
            };
            for (int i = 0; i < years.Count; i++) {
                ws.Cells[1, 4 + i].Value = years[i];
                ws.Cells[rowDict["DstYear"], 4 + i].Value = years[i];
                if (i > 0) {
                    ws.Cells[rowDict["SrcYear"], 4 + i].Value = years[i - 1];
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        public List<ScenarioSliceParameters> GetData([NotNull] string path)
        {
            if (!File.Exists(path)) {
                Info("Creating new file for alle the scenario definitions: " + path);
                var p = new ExcelPackage();
                foreach (var ws in p.Workbook.Worksheets) {
                    if ((string)ws.Cells[1, 1].Value != "Scenario") {
                        continue;
                    }

                    var d = FillMissingLines(ws);
                    FillMissingColumns(ws, d);
                }

                p.SaveAs(new FileInfo(path));
                p.Dispose();
                return new List<ScenarioSliceParameters>();
            }

            Info("Starting at  " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
            Info("Reading existing file for the scenario definitions:" + path);
            List<ScenarioSliceParameters> slices = new List<ScenarioSliceParameters>();
            using (var p = new ExcelPackage(new FileInfo(path))) {
                bool foundAllLines = true;
                foreach (var ws in p.Workbook.Worksheets) {
                    if ((string)ws.Cells[1, 1].Value != "Scenario") {
                        continue;
                    }

                    var rowDict = ReadRowDict(ws, out var row);
                    var props = GetPropertyDictionary();
                    WriteMissingPropertyLines(props, rowDict, ref foundAllLines, ws, row, p);
                    if (foundAllLines) {
                        ReadSlices(ws, rowDict, p, slices);
                    }
                }

                foreach (ScenarioSliceParameters slice in slices) {
                    if (slice.DstYear == 0 || slice.PreviousSliceNotNull.DstYear == 0) {
                        throw new Exception("Could not read scenarios properly.");
                    }
                }

                if (slices.Count == 0) {
                    throw new FlaException("Failed to read the slices");
                }

                HashSet<string> uniqueSliceCounter = new HashSet<string>();
                foreach (var slice in slices) {
                    if (uniqueSliceCounter.Contains(slice.ToString())) {
                        throw new FlaException("Duplicate slice found: " + slice);
                    }

                    uniqueSliceCounter.Add(slice.ToString());
                }

                var allScenarioShortNames = slices.Select(x => x.DstScenario.ShortName + "-" + x.DstYear).ToList();
                HashSet<string> uniqueShortNames = new HashSet<string>();
                foreach (var scenarioName in allScenarioShortNames) {
                    if (uniqueShortNames.Contains(scenarioName)) {
                        throw new FlaException("Duplicate short name found: " + scenarioName);
                    }

                    uniqueShortNames.Add(scenarioName);
                }

                return slices;
            }
        }

        [NotNull]
        private static Dictionary<string, int> FillMissingLines([NotNull] ExcelWorksheet ws)
        {
            var dict = GetPropertyDictionary();
            List<string> existingStrings = new List<string>();
            int row = 2;
            Dictionary<string, int> rowDict = new Dictionary<string, int>();
            while (!string.IsNullOrWhiteSpace((string)ws.Cells[row, 1].Value)) {
                string s = (string)ws.Cells[row, 2].Value;
                existingStrings.Add(s);
                rowDict.Add(s, row);
                row++;
            }

            dict.Sort();
            foreach (var pair in dict) {
                if (!existingStrings.Contains(pair.Name)) {
                    ws.Cells[row, 1].Value = pair.Category;
                    ws.Cells[row, 2].Value = pair.Name;
                    ws.Cells[row, 3].Value = pair.Comment;
                    ws.Cells[row, 3].Style.WrapText = true;
                    rowDict.Add(pair.Name, row);
                    row++;
                }
            }

            ws.Column(1).AutoFit();
            ws.Column(2).AutoFit();
            ws.Column(3).Width = 50;
            return rowDict;
        }

        private static int GetInteger([NotNull] ExcelWorksheet ws, [NotNull] Dictionary<string, int> rowdict, int columnOffset, [NotNull] string key)
        {
            object o = ws.Cells[rowdict[key], columnOffset + 5].Value;
            if (o is double d) {
                return (int)d;
            }

            return (int)o;
        }

        [NotNull]
        [ItemNotNull]
        private static List<Prop> GetPropertyDictionary()
        {
            Type myType = typeof(ScenarioSliceParameters);
            var properties = myType.GetProperties();
            var lst = new List<Prop>();
            foreach (var propertyInfo in properties) {
                var attributes = propertyInfo.GetCustomAttributes(true);
                ScenarioCommentAttribute sca = null;
                foreach (var attribute in attributes) {
                    if (attribute is ScenarioCommentAttribute commentattr) {
                        sca = commentattr;
                    }
                }

                Prop p = new Prop(propertyInfo.Name, sca?.Category ?? ScenarioCategory.Unbekannt, sca?.Text);
                lst.Add(p);
            }

            lst.Add(new Prop("SrcScenario", ScenarioCategory.System, ""));
            lst.Add(new Prop("SrcYear", ScenarioCategory.System, ""));
            return lst;
        }

        [NotNull]
        private static Scenario GetScenario([NotNull] ExcelWorksheet ws)
        {
            string scenarioTxt = (string)ws.Cells[1, 2].Value;
            Scenario thisScenario = new Scenario(scenarioTxt);
            return thisScenario;
        }

        [NotNull]
        private static Dictionary<string, int> ReadRowDict([NotNull] ExcelWorksheet ws, out int row)
        {
            Dictionary<string, int> rowDict = new Dictionary<string, int>();
            row = 2;
            while (ws.Cells[row, 2] != null && !string.IsNullOrWhiteSpace((string)ws.Cells[row, 2].Value)) {
                string key = ws.Cells[row, 2].Value.ToString();
                if (rowDict.ContainsKey(key)) {
                    throw new FlaException("Duplicate key: " + key);
                }

                rowDict.Add(key, row);
                row++;
            }

            return rowDict;
        }

        private void ReadSlices([NotNull] ExcelWorksheet ws,
                                [NotNull] Dictionary<string, int> rowDict,
                                [NotNull] ExcelPackage p,
                                [NotNull] [ItemNotNull] List<ScenarioSliceParameters> slices)
        {
            for (int columnOffset = 0; columnOffset < 7; columnOffset++) {
                int srcYear = GetInteger(ws, rowDict, columnOffset, "SrcYear");
                int dstYear = GetInteger(ws, rowDict, columnOffset, "DstYear");
                Scenario srcScenario = GetScenario(ws);
                var dstScenario = GetScenario(ws);
                if(dstScenario.FriendlyName.Length < 1) {
                    throw new FlaException("Unknown friendly scenario name");
                }
                Debug("Reading " + dstScenario + " " + dstYear);
                if (srcYear == 2017) {
                    srcScenario = Scenario.FromEnum(ScenarioEnum.Present);
                }

                ScenarioSliceParameters prevSlice = new ScenarioSliceParameters(srcScenario, srcYear, null);
                ScenarioSliceParameters ssp = new ScenarioSliceParameters(dstScenario, dstYear, prevSlice);
                var props = GetPropertyDictionary();
                var sliceType = typeof(ScenarioSliceParameters);
                foreach (var prop in props) {
                    if (prop.Category == ScenarioCategory.System) {
                        continue;
                    }

                    if (prop.Name == "SrcYear" || prop.Name == "SrcScenario" || prop.Name == nameof(ScenarioSliceParameters.PreviousSlice) ||
                        prop.Name == nameof(ScenarioSliceParameters.PreviousSliceNotNull)) {
                        continue;
                    }

                    if (!rowDict.ContainsKey(prop.Name)) {
                        p.Save();
                        throw new FlaException("Missing " + prop.Name + " in the excel");
                    }

                    int rowToRead = rowDict[prop.Name];
                    object o = ws.Cells[rowToRead, 5 + columnOffset].Value;
                    var propInfo = sliceType.GetProperty(prop.Name);
                    if (propInfo == null) {
                        throw new Exception("property not found: " + prop.Name);
                    }

                    propInfo.SetValue(ssp, o);
                }

                foreach (var key in rowDict.Keys) {
                    if (key == "Anzahl Jahre seit 2019" || key == "Anzahl Jahresscheiben seit 2017") {
                        continue;
                    }

                    if (props.All(x => x.Name != key)) {
                        throw new FlaException("Unused line in excel for " + key + " in scenario " + dstScenario);
                    }
                }

                slices.Add(ssp);
            }
        }

        private static void WriteMissingPropertyLines([NotNull] [ItemNotNull] List<Prop> props,
                                                      [NotNull] Dictionary<string, int> rowDict,
                                                      ref bool foundAllLines,
                                                      [NotNull] ExcelWorksheet ws,
                                                      int row,
                                                      [NotNull] ExcelPackage p)
        {
            foreach (var prop in props) {
                if (prop.Category == ScenarioCategory.System) {
                    continue;
                }

                if (prop.Name == "SrcYear" || prop.Name == "SrcScenario" || prop.Name == nameof(ScenarioSliceParameters.PreviousSlice) ||
                    prop.Name == nameof(ScenarioSliceParameters.PreviousSliceNotNull) || prop.Name == nameof(ScenarioSliceParameters.DstScenario)) {
                    continue;
                }

                if (!rowDict.ContainsKey(prop.Name)) {
                    foundAllLines = false;

                    ws.Cells[row, 1].Value = prop.Category;
                    ws.Cells[row, 2].Value = prop.Name;
                    ws.Cells[row, 3].Value = prop.Comment;
                    ws.Cells[row, 3].Style.WrapText = true;
                    p.Save();
                    row++;
                }
                else {
                    var tgtrow = rowDict[prop.Name];
                    if ((string)ws.Cells[tgtrow, 1].Value != prop.Category.ToString()) {
                        ws.Cells[tgtrow, 1].Value = prop.Category;
                        p.Save();
                    }
                }
            }
        }

        private class Prop : IComparable<Prop> {
            public Prop([NotNull] string name, ScenarioCategory category, [CanBeNull] string comment)
            {
                Name = name;
                Category = category;
                Comment = comment;
            }

            public ScenarioCategory Category { get; }

            [CanBeNull]
            public string Comment { get; }

            [NotNull]
            public string Name { get; }

            public int CompareTo([CanBeNull] Prop other)
            {
                if (ReferenceEquals(this, other)) {
                    return 0;
                }

                if (other is null) {
                    return 1;
                }

                var categoryComparison = Category.CompareTo(other.Category);
                if (categoryComparison != 0) {
                    return categoryComparison;
                }

                var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
                if (nameComparison != 0) {
                    return nameComparison;
                }

                return string.Compare(Comment, other.Comment, StringComparison.Ordinal);
            }

            [NotNull]
            public override string ToString() => Name + " (" + Category + ")";
        }
    }
}