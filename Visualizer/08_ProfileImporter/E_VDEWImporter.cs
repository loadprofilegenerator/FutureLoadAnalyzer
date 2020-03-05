using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using NPoco;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class E_VDEWImporter : RunableWithBenchmark {
        public E_VDEWImporter([NotNull] ServiceRepository services)
            : base(nameof(E_VDEWImporter), Stage.ProfileImport, 500, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<VDEWProfileValues>(Stage.ProfileImport, Constants.PresentSlice);
            var dbProfiles = SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            const string filename = @"U:\SimZukunft\RawDataForMerging\VDEWProfile.xls";
            Log(MessageType.Info, "Importing " + filename);
            dbProfiles.BeginTransaction();
            var wb = ExcelHelper.OpenXls(filename, out var app);
            for (var sheet = 1; sheet < 12; sheet++) {
                var arr = ExcelHelper.ExtractDataFromExcel(wb, sheet, "A1", "K102", out var sheetname);
                ExtractColum(sheetname, 2, Season.Winter, TagTyp.Samstag, arr, dbProfiles);
                ExtractColum(sheetname, 3, Season.Winter, TagTyp.Sonntag, arr, dbProfiles);
                ExtractColum(sheetname, 4, Season.Winter, TagTyp.Werktag, arr, dbProfiles);
                ExtractColum(sheetname, 5, Season.Sommer, TagTyp.Samstag, arr, dbProfiles);
                ExtractColum(sheetname, 6, Season.Sommer, TagTyp.Sonntag, arr, dbProfiles);
                ExtractColum(sheetname, 7, Season.Sommer, TagTyp.Werktag, arr, dbProfiles);
                ExtractColum(sheetname, 8, Season.Uebergang, TagTyp.Samstag, arr, dbProfiles);
                ExtractColum(sheetname, 9, Season.Uebergang, TagTyp.Sonntag, arr, dbProfiles);
                ExtractColum(sheetname, 10, Season.Uebergang, TagTyp.Werktag, arr, dbProfiles);
            }

            app.Quit();
            dbProfiles.CompleteTransaction();
        }

        private void ExtractColum([NotNull] string profilename, int column, Season season, TagTyp tagtyp, [NotNull] [ItemNotNull] object[,] values, [NotNull] Database dbProfiles)
        {
            var minutes = 0;
            for (var row = 4; row < 100; row++) {
                var val = Helpers.GetNoNullDouble(values[row, column]);
                var v = new VDEWProfileValues(profilename, season, minutes, val, tagtyp);
                minutes += 15;
                dbProfiles.Save(v);
            }
        }
    }
}