using System.IO;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._00_Import {
    public class ExcelHelper : BasicLoggable {
        public ExcelHelper([NotNull] ILogger logger, Stage myStage) : base(logger, myStage, nameof(ExcelHelper))
        {
        }

        [NotNull]
        [ItemCanBeNull]
        public object[,] ExtractDataFromExcel2([NotNull] string excelFileName,
                                               int worksheetIdx,
                                               [NotNull] string topleftcell,
                                               [NotNull] string bottomrightcell,
                                               [NotNull] out string sheetname)
        {
            if (!File.Exists(excelFileName)) {
                throw new FlaException("File " + excelFileName + " was not found.");
            }

            FileInfo fi = new FileInfo(excelFileName);
            if (fi.Length == 0) {
                throw new FlaException("Trying to read empty file");
            }

            using (var package = new ExcelPackage(new FileInfo(excelFileName))) {
                var workbook = package.Workbook;
                Debug("Reading values from worksheet " + worksheetIdx + " in file " + excelFileName);
                var worksheet = workbook.Worksheets[worksheetIdx];
                sheetname = worksheet.Name;
                var cell1 = worksheet.Cells[topleftcell];
                var cell2 = worksheet.Cells[bottomrightcell];
                int totalrows = cell2.End.Row - cell1.End.Row + 1;
                int totalColumns = cell2.End.Column - cell1.End.Column + 1;
                object[,] values = new object[totalrows, totalColumns];
                for (int row = 0; row < totalrows; row++) {
                    for (int col = 0; col < totalColumns; col++) {
                        values[row, col] = worksheet.Cells[row + cell1.Start.Row, col + cell1.Start.Column].Value;
                    }
                }

                return values;
            }
        }
    }
}