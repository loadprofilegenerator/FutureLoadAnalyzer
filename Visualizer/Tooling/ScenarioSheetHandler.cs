using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics.Logging;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using OfficeOpenXml;
using Xunit;
using Xunit.Abstractions;

namespace BurgdorfStatistics.Tooling
{
    public class ScenarioSheetHandlerTests {
        [NotNull] private readonly ITestOutputHelper _output;
        public ScenarioSheetHandlerTests([NotNull] ITestOutputHelper output)
        {
            _output = output;
            }
        [Fact]
        public void RunTest()

        {
            string path = "V:\\BurgdorfStatistics\\ScenarioDefinitions.xlsx";
          /*  if (File.Exists(path))
            {
                File.Delete(path);
            }*/
            var logger = new Logger(_output);
            ScenarioSheetHandler ssh = new ScenarioSheetHandler(logger);
            var slices = ssh.GetData(path);
            var u2020 = slices.Single(x => x.DstScenario == Scenario.Utopia && x.DstYear == 2020);
            string s = JsonConvert.SerializeObject(u2020, Formatting.Indented);
            _output.WriteLine(s);
            Assert.Equal(500, u2020.NumberOfNewElectricCars);
        }
    }
    public class ScenarioSheetHandler {
        [NotNull] private Logger _logger;

        public ScenarioSheetHandler([NotNull] Logger logger)
        {
            _logger = logger;
        }


        private class Prop :IComparable<Prop> {
            public Prop([NotNull] string name, ScenarioCategory category, [CanBeNull] string comment)
            {
                Name = name;
                Category = category;
                Comment = comment;
            }

            public override string ToString() => Name + " (" + Category+ ")";

            [NotNull]
            public string Name { get;  }
            public ScenarioCategory Category { get;  }
            [CanBeNull]
            public string Comment { get;  }

            public int CompareTo([CanBeNull] Prop other)
            {
                if (ReferenceEquals(this, other)) {
                    return 0;
                }

                if (ReferenceEquals(null, other)) {
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
        }
        [NotNull]
        [ItemNotNull]
        private List<Prop> GetPropertyDictionary()
        {
            Type myType = typeof(ScenarioSliceParameters);
            var properties = myType.GetProperties();
            var lst = new List<Prop>();
            foreach (var propertyInfo in properties)
            {
                var attributes = propertyInfo.GetCustomAttributes(true);
                ScenarioCommentAttribute sca = null;
                foreach (var attribute in attributes)
                {
                    if (attribute is ScenarioCommentAttribute commentattr) {
                        sca= commentattr;
                    }
                }
                Prop p = new Prop(propertyInfo.Name,sca?.Category??ScenarioCategory.Unbekannt, sca?.Text);
                lst.Add(p);
            }
            lst.Add(new Prop("SrcScenario",ScenarioCategory.System,""));
            lst.Add(new Prop("SrcYear", ScenarioCategory.System, ""));
            return lst;
        }
        [NotNull]
        private Dictionary<string,int> FillMissingLines([NotNull] ExcelWorksheet ws)
        {
            var dict = GetPropertyDictionary();
            List<string> existingStrings = new List<string>();
            ws.Cells[1, 1].Value = "Year";
            int row = 2;
            Dictionary<string,int> rowDict = new Dictionary<string, int>();
            while (!string.IsNullOrWhiteSpace((string)ws.Cells[row, 1].Value)) {
                string s = (string)ws.Cells[row, 2].Value;
                existingStrings.Add(s);
                rowDict.Add(s,row);
                row++;
            }

            dict.Sort();
            foreach (var pair in dict) {
                if (!existingStrings.Contains(pair.Name)) {
                    ws.Cells[row, 1].Value = pair.Category;
                    ws.Cells[row, 2].Value = pair.Name;
                    ws.Cells[row, 3].Value = pair.Comment;
                    ws.Cells[row, 3].Style.WrapText = true;
                    rowDict.Add(pair.Name,row);
                    row++;
                }
            }
            ws.Column(1).AutoFit();
            ws.Column(2).AutoFit();
            ws.Column(3).Width = 50;
            return rowDict;
        }


        public void FillMissingColumns([NotNull] ExcelWorksheet ws, [NotNull] Dictionary<string,int> rowDict)
        {
            List<int> years = new List<int>() {
                2017, 2020, 2025, 2030, 2035, 2040, 2045, 2050
            };
            for (int i = 0; i < years.Count; i++) {
                ws.Cells[1, 4+i].Value = years[i];
                ws.Cells[rowDict["DstScenario"], 4 + i].Value = ws.Name;
                ws.Cells[rowDict["SrcScenario"], 4 + i].Value = ws.Name;
                ws.Cells[rowDict["DstYear"], 4 + i].Value = years[i];
                if (i > 0) {
                    ws.Cells[rowDict["SrcYear"], 4 + i].Value = years[i-1];
                }

            }

        }
        [NotNull]
        [ItemNotNull]
        public List<ScenarioSliceParameters> GetData([NotNull] string path)
        {
            if (!File.Exists(path)) {
                _logger.Info("Creating new file for alle the scenario definitions: " + path);
                var p = new ExcelPackage();
                //                List<ExcelWorksheet> worksheets = new List<ExcelWorksheet>();
                foreach (Scenario scenario in ScenarioHelper.UsedScenarios) {
                    var ws = p.Workbook.Worksheets.Add(scenario.ToString());
                //    worksheets.Add(ws);
                    var d = FillMissingLines(ws);
                    FillMissingColumns(ws,d);
                }
                p.SaveAs(new FileInfo(path));
                p.Dispose();
                return new List<ScenarioSliceParameters>();
            }
            else {
                _logger.Info("Reading existing file for the scenario definitions:"  + path);
                List< ScenarioSliceParameters> slices = new List<ScenarioSliceParameters>();
                var p = new ExcelPackage(new FileInfo(path));
                List<string> validWs = new List<string> {"Pom", "Nep","Utopia","Dystopia"};
                foreach (var ws in p.Workbook.Worksheets) {
                    if (!validWs.Contains(ws.Name)) {
                        continue;
                    }
                    Dictionary<string, int> rowDict = new Dictionary<string, int>();
                    int row = 2;
                    while (ws.Cells[row, 2]!=null && !string.IsNullOrWhiteSpace((string)ws.Cells[row,2].Value)) {
                        string key = ws.Cells[row, 2].Value.ToString();
                        rowDict.Add(key,row);
                        row++;
                    }
                    for (int columnOffset = 0; columnOffset < 7; columnOffset++) {
                        int srcYear = GetInteger(ws, rowDict, columnOffset, "SrcYear");
                        int dstYear = GetInteger(ws, rowDict, columnOffset, "DstYear");
                        Scenario srcScenario = GetScenario(ws, rowDict, columnOffset, "SrcScenario");
                        Scenario dstScenario = GetScenario(ws, rowDict, columnOffset, "DstScenario");
                        _logger.Info("Reading " + dstScenario + " " + dstYear);
                        if (srcYear == 2017) {
                            srcScenario = Scenario.Present;
                        }
                        ScenarioSliceParameters prevSlice = new ScenarioSliceParameters(srcScenario,srcYear,null);
                        ScenarioSliceParameters ssp = new ScenarioSliceParameters(dstScenario, dstYear,prevSlice);
                        var props = GetPropertyDictionary();
                        var sliceType = typeof(ScenarioSliceParameters);
                        foreach (var prop in props) {
                            if (prop.Category == ScenarioCategory.System) {
                                continue;
                            }

                            if (prop.Name == "SrcYear" || prop.Name == "SrcScenario"|| prop.Name =="PreviousScenarioNotNull"
                                || prop.Name == "PreviousScenario")
                            {
                                continue;
                            }
                            int rowToRead = rowDict[prop.Name];
                            object o = ws.Cells[rowToRead, 5 + columnOffset].Value;
                            var propInfo = sliceType.GetProperty(prop.Name);
                            if (propInfo == null) {
                                throw new Exception("property not found: " + prop.Name);
                            }
                            propInfo.SetValue(ssp, o);
                        }
                        slices.Add(ssp);
                    }
                }

                foreach (ScenarioSliceParameters slice in slices) {
                    if (slice.DstYear == 0 || slice.PreviousScenarioNotNull.DstYear == 0) {
                        throw new Exception("Could not read scenarios properly.");
                    }
                }
                return slices;
            }
        }

        private int GetInteger([NotNull] ExcelWorksheet ws, [NotNull] Dictionary<string, int> rowdict, int columnOffset, [NotNull] string key)
        {
            object o = ws.Cells[rowdict[key], columnOffset + 5].Value;
            if (o is double d) {
                return (int)d;
            }
            return (int)o;
        }
        private Scenario GetScenario([NotNull] ExcelWorksheet ws, [NotNull] Dictionary<string, int> rowdict, int columnOffset, [NotNull] string key)
        {
            string scenarioTxt = (string)ws.Cells[rowdict[key], columnOffset + 5].Value;
            Scenario thisScenario = (Scenario)Enum.Parse(typeof(Scenario), scenarioTxt);
            return thisScenario;
        }
    }
}
