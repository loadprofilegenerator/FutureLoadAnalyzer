using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class D_RLMAssigner : RunableWithBenchmark {
        public D_RLMAssigner([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(D_RLMAssigner), Stage.ProfileImport, 400, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<LastgangBusinessAssignment>(Stage.ProfileImport, Constants.PresentSlice);
            var dbProfiles = SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;

            dbProfiles.BeginTransaction();
            const string filename = @"U:\SimZukunft\RawDataForMerging\BusinessRLMProfileAssignments.xlsx";
            Log(MessageType.Info, "Importing " + filename);
            var arr = ExcelHelper.ExtractDataFromExcel(filename, 1, "A1", "G100");

            //read header
            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1];
                if (o == null) {
                    continue;
                }

                hdict.Add(o.ToString(), i + 1);
            }

            for (var row = 2; row < arr.GetLength(0); row++) {
                if (arr[row, hdict["Datei"]] == null) {
                    continue;
                }

                var rba = new LastgangBusinessAssignment(Helpers.GetStringNotNull(arr[row, hdict["Datei"]]),
                    Helpers.GetStringNotNull(arr[row, hdict["ComplexName"]]),
                    Helpers.GetStringNotNull(arr[row, hdict["BusinessName"]]),
                    Helpers.GetStringNotNull(arr[row, hdict["ErzeugerID"]])
                    ) {
                    Standort = Helpers.GetStringNotNull(arr[row, hdict["Standort"]])
                };
                dbProfiles.Save(rba);
            }

            dbProfiles.CompleteTransaction();
            var assignments = dbProfiles.Fetch<LastgangBusinessAssignment>();
            var rlmprofiles = dbProfiles.Fetch<RlmProfile>();
            var assignedNames = assignments.Select(x => x.RlmFilename).ToList();
            var allFound = true;
            foreach (var profile in rlmprofiles) {
                if (!assignedNames.Contains(profile.Name)) {
                    Log(MessageType.Info, profile.Name + "\t" + profile.SumElectricity);
                    allFound = false;
                }
            }

            if (!allFound) {
                throw new Exception("Unassigend rlm profiles");
            }
        }
    }
}