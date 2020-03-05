using System;
using System.Collections.Generic;
using System.IO;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling {
    // ReSharper disable once InconsistentNaming
    public static class ZZ_ProfileImportHelper {
        [NotNull]
        public static Profile ReadCSV([NotNull] string filename, [NotNull] string profilename)
        {
            var vals = new List<double>();
            using (var sr = new StreamReader(filename)) {
                while (!sr.EndOfStream) {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) {
                        var d = Convert.ToDouble(line);
                        vals.Add(d);
                    }
                }
            }

            var p = new Profile(profilename, vals.AsReadOnly(), EnergyOrPower.Power);
            return p;
        }
    }
}