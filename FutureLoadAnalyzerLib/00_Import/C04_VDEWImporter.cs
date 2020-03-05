using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class C04_VDEWImporter : RunableWithBenchmark {
        public C04_VDEWImporter([NotNull] ServiceRepository services) : base(nameof(C04_VDEWImporter), Stage.Raw, 204, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            var dbProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            dbProfiles.RecreateTable<VDEWProfileValue>();
            string filename = CombineForFlaSettings("VDEWProfile.xlsx");
            Info("Importing " + filename);
            dbProfiles.BeginTransaction();
            //var wb = ExcelHelper.OpenXls(filename, out var app);
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            for (var sheet = 1; sheet < 12; sheet++) {
                var arr = eh.ExtractDataFromExcel2(filename, sheet, "A1", "K102", out var sheetname);
                ExtractColum(sheetname, 1, Season.Winter, TagTyp.Samstag, arr, dbProfiles);
                ExtractColum(sheetname, 2, Season.Winter, TagTyp.Sonntag, arr, dbProfiles);
                ExtractColum(sheetname, 3, Season.Winter, TagTyp.Werktag, arr, dbProfiles);
                ExtractColum(sheetname, 4, Season.Sommer, TagTyp.Samstag, arr, dbProfiles);
                ExtractColum(sheetname, 5, Season.Sommer, TagTyp.Sonntag, arr, dbProfiles);
                ExtractColum(sheetname, 6, Season.Sommer, TagTyp.Werktag, arr, dbProfiles);
                ExtractColum(sheetname, 7, Season.Uebergang, TagTyp.Samstag, arr, dbProfiles);
                ExtractColum(sheetname, 8, Season.Uebergang, TagTyp.Sonntag, arr, dbProfiles);
                ExtractColum(sheetname, 9, Season.Uebergang, TagTyp.Werktag, arr, dbProfiles);
            }

            dbProfiles.CompleteTransaction();
        }

        private static void ExtractColum([NotNull] string profilename,
                                         int column,
                                         Season season,
                                         TagTyp tagtyp,
                                         [NotNull] [ItemNotNull] object[,] values,
                                         [NotNull] MyDb dbProfiles)
        {
            var minutes = 0;
            for (var row = 3; row < 99; row++) {
                var val = Helpers.GetNoNullDouble(values[row, column]);
                var v = new VDEWProfileValue(profilename, season, minutes, val, tagtyp);
                minutes += 15;
                dbProfiles.Save(v);
            }
        }
    }
}