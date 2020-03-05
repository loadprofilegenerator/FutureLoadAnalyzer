using BurgdorfStatistics.Tooling;
using Common.Steps;
using Data.DataModel.Export;
using JetBrains.Annotations;

namespace BurgdorfStatistics._09_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class A01_ExportTablePreparer : RunableForSingleSliceWithBenchmark {
        public A01_ExportTablePreparer([NotNull] ServiceRepository services)
            : base(nameof(A01_ExportTablePreparer), Stage.ProfileGeneration, 100, services, false)
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration,
                parameters);

            var fieldDefinition = Prosumer.GetSaveableEntry(dbDstProfiles, TableType.HousePart);
            fieldDefinition.MakeTableForListOfFields();
//            Services.SqlConnection.RecreateTable<Prosumer>(Stage.ProfileGeneration, parameters.DstScenario, parameters.DstYear);
            dbDstProfiles.Database.Execute("VACUUM");
        }
    }
}