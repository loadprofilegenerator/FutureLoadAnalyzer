using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.ProfileImport;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class A_BKWProfileImport : RunableWithBenchmark {
        public A_BKWProfileImport([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(A_BKWProfileImport), Stage.ProfileImport, 100, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            const string filename = @"U:\SimZukunft\RawDataForMerging\stadtprofil_15min_auflösung.csv";
            const string profilename = "01-bkwlast";
            var bkwRaw = ZZ_ProfileImportHelper.ReadCSV(filename, profilename);
            Services.SqlConnection.RecreateTable<BkwProfile>(Stage.ProfileImport, Constants.PresentSlice);
            var bkp = new BkwProfile {
                Profile = bkwRaw,
                Name = "BKW übergabe"
            };

            var db = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            db.BeginTransaction();
            db.Save(bkp);
            db.CompleteTransaction();
        }
    }
}