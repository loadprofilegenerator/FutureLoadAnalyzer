using System.IO;
using Common;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once InconsistentNaming
    public class E01_ScenarioDefinitionSheetFixer : RunableWithBenchmark {
        protected override void RunActualProcess()
        {
            string csvName = CombineForFlaSettings("ScenarioDefinitions.xlsx");
            var dstfn =  MakeAndRegisterFullFilename("ScenarioDefinitionsForBericht.xlsx",Constants.PresentSlice);
            using (var srcPackage = new ExcelPackage(new FileInfo(csvName))) {
                using (var dstPackage = new ExcelPackage(new FileInfo(dstfn))) {
                    CopyOneSheet(srcPackage, dstPackage, "Pom");
                    CopyOneSheet(srcPackage, dstPackage, "Nep");
                    CopyOneSheet(srcPackage, dstPackage, "Utopia");
                    CopyOneSheet(srcPackage, dstPackage, "Dystopia");
                    dstPackage.Save();
                }
            }

            SaveToPublicationDirectory(dstfn,Constants.PresentSlice,"4.3");
        }

        private static void CopyOneSheet([NotNull] ExcelPackage srcPackage, [NotNull] ExcelPackage dstPackage, [NotNull] string sheetname)
        {
            ExcelWorksheet srcWs = srcPackage.Workbook.Worksheets[sheetname];
            ExcelWorksheet dstWs = dstPackage.Workbook.Worksheets.Add(sheetname);
            int srcrow = 6;
            int dstRow = 2;
            CopyOneRow(dstWs,  1, srcWs, 1);
            while (srcWs.Cells[srcrow, 1].Value != null) {
                CopyOneRow(dstWs, dstRow,  srcWs, srcrow);
                srcrow++;
                dstRow++;
            }
            dstWs.Cells[1, 1].Value = sheetname.ToUpper();
        }

        private static void CopyOneRow([NotNull] ExcelWorksheet dstWs, int dstRow,  [NotNull] ExcelWorksheet srcWs, int srcrow)
        {
            int dstcol = 1;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 3].Value;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 5].Value;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 6].Value;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 7].Value;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 8].Value;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 9].Value;
            dstWs.Cells[dstRow, dstcol++].Value = srcWs.Cells[srcrow, 10].Value;
            dstWs.Cells[dstRow, dstcol].Value = srcWs.Cells[srcrow, 11].Value;
            dstWs.Cells[dstRow, 1, dstRow, 11].Style.Numberformat.Format = GetFormatString((string)dstWs.Cells[dstRow,1].Value) ;
        }

        [NotNull]
        private static string GetFormatString([CanBeNull] string key)
        {
            switch (key) {
                case "Reduktion Energieverbrauch Businesses (Prozent, über 5 Jahre, 0-1)": return "0.0%";
                case "Reduktion Energieverbrauch Haushalte (Prozent, über 5 Jahre, 0-1)": return "0.0%";
                case "Anteil an Flächen die klimatisiert sind": return "0.0%";
                case "Prozent Autobesitzer (0.44 = 440/1000 Haushalten haben ein Auto) [%]": return "0.0%";
                case "Prozent der Elektroautos [%]": return "0.0%";
                case "Haus Energie Renovierungsfaktor (0.2 = von 100 MWh Verbrauch bleiben noch 20 MWh) [%]": return "0.0%";
                case "Reduktion Stromverbrauch Gebäudeinfrastruktur (Prozent, über 5 Jahre, 0-1, 0.99 = 1% Reduktion)": return "0.0%";
                case "Wie viele Heizungen von Öl auf Wärmepumpen umgestellt werden, in % des 2017 Energieverbrauchs": return "0.0%";
                case "Wie viele Heizungen von Gas auf Wärmepumpen umgestellt werden, in % des 2017 Energieverbrauchs": return "0.0%";
                case "Wie viele Heizungen von Other auf Wärmepumpen umgestellt werden, in % des 2017 Energieverbrauchs": return "0.0%";
                case "Prozent der Gebäudesanierungen [%, 0-1]": return "0.0%";
                default: return "0.0";
            }
        }

        public E01_ScenarioDefinitionSheetFixer([NotNull] ServiceRepository services)
            : base(nameof(E01_ScenarioDefinitionSheetFixer), Stage.Raw, 501, services, true)
        {
        }
    }
}