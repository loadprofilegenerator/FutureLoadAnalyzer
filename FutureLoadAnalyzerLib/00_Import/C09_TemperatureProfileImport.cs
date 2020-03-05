using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once InconsistentNaming
    public class C09_TemperatureProfileImport : RunableWithBenchmark {
        public C09_TemperatureProfileImport([NotNull] ServiceRepository services) : base(nameof(C09_TemperatureProfileImport),
            Stage.Raw,
            209,
            services,
            true)
        {
        }

        protected override void RunActualProcess()
        {
            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<TemperatureProfileImport>();
            db.BeginTransaction();
            db.Save(ReadOneProfile("Bern-temperatures-hour-2010.csv", 2017));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2020.csv", 2020));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2020.csv", 2025));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2030.csv", 2030));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2030.csv", 2035));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2040.csv", 2040));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2040.csv", 2045));
            db.Save(ReadOneProfile("Bern-temperatures-hour-2050.csv", 2050));
            db.CompleteTransaction();
        }

        [NotNull]
        private TemperatureProfileImport ReadOneProfile([NotNull] string filename, int year)
        {
            var temps = ReadTemperatures(CombineForFlaSettings(filename));
            if (temps.Count == 8784) {
                temps = temps.Take(8760).ToList();
            }

            if (temps.Count != 8760) {
                throw new FlaException("Invalid value count: " + temps.Count);
            }

            JsonSerializableProfile jsp = new JsonSerializableProfile(filename, temps.AsReadOnly(), EnergyOrPower.Temperatures);
            TemperatureProfileImport tpi = new TemperatureProfileImport(filename, year, jsp);
            return tpi;
        }

        [NotNull]
        private static List<double> ReadTemperatures([NotNull] string filename)
        {
            //read csv
            using (StreamReader sr = new StreamReader(filename)) {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                var values = new List<double>();
                while (!sr.EndOfStream) {
                    string line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) {
                        double val = Convert.ToDouble(line);
                        values.Add(val);
                    }
                }

                return values;
            }
        }
    }
}