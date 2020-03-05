using System.IO;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B04_HouseProfileXlsxExporter : RunableForSingleSliceWithBenchmark {
        public B04_HouseProfileXlsxExporter([NotNull] ServiceRepository services) : base(nameof(B04_HouseProfileXlsxExporter),
            Stage.ProfileAnalysis,
            204,
            services,
            false)
        {
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            if (!Services.RunningConfig.MakeExcelPerTrafokreis) {
                return;
            }

            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedHouseProfiles, Services.Logger);
            string currentTrafokreis = "";
            ExcelWorksheet ws;
            int columnIdx = 1;
            ExcelPackage p = new ExcelPackage();
            ws = p.Workbook.Worksheets.Add("sheet1");
            foreach (var house in saHouses.ReadEntireTableDBAsEnumerable("Trafokreis")) {
                if (currentTrafokreis != house.Trafokreis && !string.IsNullOrWhiteSpace(currentTrafokreis)) {
                    var fn = MakeAndRegisterFullFilename(FilenameHelpers.CleanFileName(currentTrafokreis) + ".xlsx", slice);
                    // ReSharper disable once PossibleNullReferenceException
                    p.SaveAs(new FileInfo(fn));
                    SaveToArchiveDirectory(fn, RelativeDirectory.Trafokreise, slice);
                    p.Dispose();
                    p = new ExcelPackage();
                    ws = p.Workbook.Worksheets.Add(currentTrafokreis);
                    columnIdx = 2;
                }

                currentTrafokreis = house.Trafokreis;
                // ReSharper disable once PossibleNullReferenceException
                ws.Cells[1, columnIdx].Value = house.Name;
                int rowIdx = 2;
                for (int i = 0; i < house.Profile.Values.Count; i++) {
                    ws.Cells[rowIdx, columnIdx].Value = house.Profile.Values[i];
                    rowIdx++;
                }

                columnIdx++;
            }

            var fn2 = MakeAndRegisterFullFilename(FilenameHelpers.CleanFileName(currentTrafokreis) + ".xlsx", slice);
            // ReSharper disable once PossibleNullReferenceException
            p.SaveAs(new FileInfo(fn2));
            SaveToArchiveDirectory(fn2, RelativeDirectory.Trafokreise, slice);

            p.Dispose();
        }
    }
}