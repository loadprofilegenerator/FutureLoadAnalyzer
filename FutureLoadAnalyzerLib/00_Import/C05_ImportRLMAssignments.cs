using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class C05_ImportRLMAssignments : RunableWithBenchmark {
        public C05_ImportRLMAssignments([NotNull] ServiceRepository services) : base(nameof(C05_ImportRLMAssignments),
            Stage.Raw,
            205,
            services,
            false)
        {
        }

        protected override void RunActualProcess()
        {
            var dbProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            dbProfiles.RecreateTable<LastgangBusinessAssignment>();
            dbProfiles.BeginTransaction();
            string filename = CombineForFlaSettings("BusinessRLMProfileAssignments.xlsx");
            Info("Importing " + filename);
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(filename, 1, "A1", "G100", out var _);

            //read header
            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1); i++) {
                var o = arr[0, i];
                if (o == null) {
                    continue;
                }

                hdict.Add(o.ToString(), i);
            }

            var usedKeys = new List<string>();
            for (var row = 1; row < arr.GetLength(0); row++) {
                if (arr[row, hdict["Datei"]] == null) {
                    continue;
                }

                var complexname = Helpers.GetString(arr[row, hdict["ComplexName"]]);
                var businessname = Helpers.GetString(arr[row, hdict["BusinessName"]]);
                var erzeugerid = Helpers.GetString(arr[row, hdict["ErzeugerID"]]);
                var standort = Helpers.GetString(arr[row, hdict["Standort"]]);
                string key = complexname + " " + businessname + " " + erzeugerid + " " + standort + " ";
                if (usedKeys.Contains(key) && !key.Contains("none")) {
                    throw new FlaException("key already used: " + key);
                }

                usedKeys.Add(key);
                var fn = Helpers.GetString(arr[row, hdict["Datei"]]);
                var rba = new LastgangBusinessAssignment(fn, complexname, businessname, erzeugerid) {
                    Standort = standort
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
                    Info("Unassigned Profile: " + profile.Name + "\t" + profile.SumElectricity);
                    allFound = false;
                }
            }

            if (!allFound) {
                throw new Exception("Unassigend rlm profiles");
            }

            foreach (var rlm in assignments) {
                if (rlm.ComplexName == "none") {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rlm.BusinessName + rlm.Standort + rlm.ErzeugerID)) {
                    throw new FlaException("assignment without full info: " + rlm.RlmFilename);
                }
            }
        }
    }
}