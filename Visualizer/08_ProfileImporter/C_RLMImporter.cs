using System;
using System.Collections.Generic;
using System.IO;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using NPoco;

namespace BurgdorfStatistics._08_ProfileImporter {
    // ReSharper disable once InconsistentNaming
    public class C_RLMImporter : RunableWithBenchmark {
        public C_RLMImporter([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(C_RLMImporter), Stage.ProfileImport, 300, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<RlmProfile>(Stage.ProfileImport, Constants.PresentSlice);
            var db = SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;

            db.BeginTransaction();
            var di = new DirectoryInfo(@"U:\SimZukunft\RawDataForMerging\RLMProfiles");
            var files = di.GetFiles("*.xlsx");
            var filesToIgnore = new List<string> {
                "PV FHS(gültig ab 24.1.2018) - 38256037.xlsx",
                "TS_Buchmattstrasse_10_(Baujahr_2017)_21803_Bezug.xlsx",
                "Ypsomed AG_Lochbachstrasse_ 26_3414Oberburg_06625_Abgabe.xlsx"
            };
            foreach (var file in files) {
                if (filesToIgnore.Contains(file.Name)) {
                    continue;
                }

                ImportOneProfile(file, db);
            }

            db.CompleteTransaction();
        }

        private void ImportOneProfile([JetBrains.Annotations.NotNull] FileInfo filename, [JetBrains.Annotations.NotNull] Database db)
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

            Log(MessageType.Info, "Importing " + filename.Name);
            var arr = ExcelHelper.ExtractDataFromExcel(filename.FullName, 2, "A1", "F35040");

            var hdict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i + 1];
                if (o == null) {
                    throw new Exception("Value was null");
                }

                hdict.Add(o.ToString(), i + 1);
            }

            var vals = new double[35040];
            var dtlookup = new Dictionary<DateTime, int>();
            var d = new DateTime(2017, 1, 1);
            for (var i = 0; i < 35040; i++) {
                dtlookup.Add(d, i);
                d = d.AddMinutes(15);
            }

            for (var row = 2; row < arr.GetLength(0); row++) {
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

            var a = new RlmProfile {
                Name = filename.Name
            };
            var lstvals = new List<double>(vals);
            a.Profile = new Profile(filename.Name, lstvals.AsReadOnly(), ProfileType.Power);
            db.Save(a);
        }
    }
}