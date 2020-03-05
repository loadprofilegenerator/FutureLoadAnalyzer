//using System;

using System.IO;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace BurgdorfStatistics.Exporter {
    public class GebäudeIDExporter : RunableWithBenchmark {
        public GebäudeIDExporter([NotNull] ServiceRepository services)
            : base(nameof(GebäudeIDExporter), Stage.ValidationExporting, 2, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            //string excelFileName = @"U:\SimZukunft\RawDataForMerging\DatenfürEnergiebilanzBurgdorf2018.xlsx";
            //var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw,Constants.PresentSlice).Database;
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            //var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;

            var complexes = dbComplexes.Fetch<BuildingComplex>();
            //var complexEnergy = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
            //new Random(1);
            using (var p = new ExcelPackage()) {
                //A workbook must have at least on cell, so lets add one...
                var ws = p.Workbook.Worksheets.Add("MySheet");
                //To set values in the spreadsheet use the Cells indexer.
                //header
                ws.Cells[1, 1].Value = "Gebäudekomplex-Name";
                ws.Cells[1, 2].Value = "Adressen";
                ws.Cells[1, 3].Value = "EGIDs";
                ws.Cells[1, 4].Value = "ISN";

                var row = 2;
                foreach (var complex in complexes) {
                    if (complex.GebäudeObjectIDs.Count > 0) {
                        WriteOneLine(ws, ref row, complex);
                    }
                }

                //Save the new workbook. We haven't specified the filename so use the Save as method.
                p.SaveAs(new FileInfo(@"d:\myworkbook.xlsx"));
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        private void WriteOneLine([NotNull] ExcelWorksheet ws, ref int row, [NotNull] BuildingComplex complex)
        {
            foreach (var gebäudeObjectID in complex.GebäudeObjectIDs) {
                ws.Cells[row, 1].Value = complex.ComplexName;
                ws.Cells[row, 2].Value = complex.AdressesAsJson.Replace("[", "").Replace("]", "");
                ws.Cells[row, 3].Value = complex.EGIDsAsJson.Replace("[", "").Replace("]", "");
                ws.Cells[row, 4].Value = gebäudeObjectID;
                row++;
            }
        }

        /*
        [NotNull]
        private string CollapseList([ItemNotNull] [NotNull] IEnumerable<string> strs)
        {
            string s = "";
            var builder = new System.Text.StringBuilder();
            builder.Append(s);
            foreach (string str in strs)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (str != null)
                {
                    builder.Append(str.Trim() + ", ");
                }
            }
            s = builder.ToString();
            if (s.EndsWith(", "))
            {
                s = s.Substring(0, s.Length - 2);
            }
            return s;
        }
        */
    }
}