using Common;
using Common.Steps;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class C02_BKWProfileImport : RunableWithBenchmark {
        public C02_BKWProfileImport([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(C02_BKWProfileImport), Stage.Raw, 202, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            string fn = CombineForRaw("stadtprofil_15min_auflösung.csv");
            const string profilename = "01-bkwlast";
            var bkwRaw = ZZ_ProfileImportHelper.ReadCSV(fn, profilename);
            JsonSerializableProfile jsp = new JsonSerializableProfile(bkwRaw);
            var bkp = new BkwProfile {
                Profile = jsp,
                Name = "BKW übergabe"
            };

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<BkwProfile>();
            db.BeginTransaction();
            db.Save(bkp);
            db.CompleteTransaction();
        }
    }
}