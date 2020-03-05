using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class C03_RLMImporter : RunableWithBenchmark {
        public C03_RLMImporter([NotNull] ServiceRepository services) : base(nameof(C03_RLMImporter), Stage.Raw, 203, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<RlmProfile>();
            db.BeginTransaction();
            string dir = CombineForRaw("RLMProfiles");
            var di = new DirectoryInfo(dir);
            var files = di.GetFiles("*.xlsx");
            var filesToIgnore = new List<string> {
                "PV FHS(gültig ab 24.1.2018) - 38256037.xlsx",
                "TS_Buchmattstrasse_10_(Baujahr_2017)_21803_Bezug.xlsx"
            };
            foreach (var file in files) {
                if (filesToIgnore.Contains(file.Name)) {
                    continue;
                }

                var a = ImportOneProfile(file);
                db.Save(a);
            }

            db.CompleteTransaction();
            var rlms = db.Fetch<RlmProfile>();
            foreach (var rlm in rlms) {
                if (rlm.Profile.Values.Count == 0) {
                    throw new FlaException("Loading the rlm profile didn't work properly'");
                }
            }
        }

        [NotNull]
        private RlmProfile ImportOneProfile([NotNull] FileInfo filename)
        {
            var multiplier = 0;

            if (filename.Name.Contains("_Bezug")) {
                multiplier = -1;
            }

            if (filename.Name.Contains("_Abgabe")) {
                multiplier = 1;
            }

            if (multiplier == 0) {
                throw new Exception("Unknown profile type:" + filename.Name);
            }

            Info("Importing " + filename.Name);
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(filename.FullName, 2, "A1", "F35041", out var _);

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1); i++) {
                var o = arr[0, i];
                if (o == null) {
                    continue;
                }

                hdict.Add(o.ToString(), i);
            }

            var vals = new double[35040];
            var dtlookup = new Dictionary<DateTime, int>();
            var d = new DateTime(2017, 1, 1);
            for (var i = 0; i < 35040; i++) {
                dtlookup.Add(d, i);
                d = d.AddMinutes(15);
            }

            for (var row = 1; row < arr.GetLength(0); row++) {
                if (arr[row, hdict["Zeitpunkt (Beginn Messung)"]] == null) {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(arr[row, hdict["Zeitpunkt (Beginn Messung)"]].ToString())) {
                    continue;
                }

                var dt = Helpers.GetDateTime(arr[row, hdict["Zeitpunkt (Beginn Messung)"]]);
                var idx = dtlookup[dt];
                vals[idx] = multiplier * Helpers.GetNoNullDouble(arr[row, hdict["Wert"]]);
                var unit = Helpers.GetString(arr[row, hdict["Einheit"]]);
                if (unit != "kW") {
                    throw new Exception("unit not kw in file:" + filename.FullName);
                }
            }

            var lstvals = new List<double>(vals);
            var prof = new JsonSerializableProfile(filename.Name, lstvals.AsReadOnly(), EnergyOrPower.Power);
            var a = new RlmProfile(filename.Name, 0, prof);
            if (a.Profile.Values.Any(x => double.IsNaN(x))) {
                throw new FlaException("Profile was broken: " + filename.FullName);
            }

            return a;
        }
    }
}