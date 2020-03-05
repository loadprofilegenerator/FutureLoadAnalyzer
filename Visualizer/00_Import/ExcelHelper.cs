using JetBrains.Annotations;
using Microsoft.Office.Interop.Excel;

namespace BurgdorfStatistics._00_Import {
    public static class ExcelHelper {
        [NotNull]
        [ItemCanBeNull]
        public static object[,] ExtractDataFromExcel([NotNull] string excelFileName, int worksheet, [NotNull] string topleftcell, [NotNull] string bottomrightcell)
        {
            var app = new Application {
                Visible = true
            };
            var book1 = app.Workbooks.Open(excelFileName);
            var sheet1 = (Worksheet)book1.Worksheets[worksheet];
            var range = sheet1.get_Range(topleftcell, bottomrightcell); //AG475820

            object value = range.Value; //the value is boxed two-dimensional array
            var arr = (object[,])value;
            app.Quit();
            return arr;
        }

        [NotNull]
        public static Workbook OpenXls([NotNull] string excelFileName, [NotNull] out Application app)
        {
            app = new Application {
                Visible = true
            };
            var book1 = app.Workbooks.Open(excelFileName);
            return book1;
        }

        [NotNull]
        [ItemCanBeNull]
        public static object[,] ExtractDataFromExcel([NotNull] Workbook book1, int worksheet, [NotNull] string topleftcell, [NotNull] string bottomrightcell, [NotNull] out string sheetname)
        {
            var sheet1 = (Worksheet)book1.Worksheets[worksheet];
            var range = sheet1.get_Range(topleftcell, bottomrightcell); //AG475820

            object value = range.Value; //the value is boxed two-dimensional array
            var arr = (object[,])value;
            sheetname = sheet1.Name;
            return arr;
        }
    }
}