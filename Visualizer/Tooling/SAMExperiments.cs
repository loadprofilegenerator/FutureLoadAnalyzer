/*using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BurgdorfStatistics.Tooling.SAM;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BurgdorfStatistics.Tooling {
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "InlineOutVariableDeclaration")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SAMExperiments {
        private readonly Logging.Logger _logger;
        public SAMExperiments(ITestOutputHelper output) => _logger = new Logging.Logger(output);

        private class PVSystemSettings {
            private readonly Logging.Logger _logger;

            public PVSystemSettings(float tilt, float azimut, float acPower, float dcAcRatio, Logging.Logger logger)
            {
                Tilt = tilt;
                this.Azimut = azimut;
                AcPower = acPower;
                this.DcAcRatio = dcAcRatio;
                _logger = logger;
            }

            public float Tilt { get; }
            public float Azimut { get; }
            public float AcPower { get; }
            public float DcAcRatio { get; }

            public Results Result { get; set; }

            public void Run()
            {
                var data = new Data();
                var module = InitializeModule(data, Tilt, Azimut, AcPower, DcAcRatio);
                Result = ExecuteCalculation(module, data, Tilt, Azimut, _logger);
            }

            public Thread MyThread { get; set; }
        }

        [Fact]
        public void RunTiltAnalysisFull()
        {
            Directory.SetCurrentDirectory(@"V:\Dropbox\BurgdorfStatistics\Sam");
            _logger.Info("SSC Version number = " + API.Version());
            _logger.Info("SSC bBuild Information = " + API.BuildInfo());
            Module.SetPrint(0);


            //info
            //get variable info
            //Info info = new Info(module);
            //while (info.Get())
            // {
            //   Console.Write(info.Name() + ": ");
            // Log(info.Constraints() + ":" + info.Label() + ":" +info.DataType());
            //}

            var tilts = new List<float> {
                0, 10, 20,
                30, 40, 50, 60, 70, 80, 90
            };
            var azimuts = new List<float>();
            for (var i = 0; i < 360; i += 10) {
                azimuts.Add(i);
            }

            const float dcacratio = 2f;
            const float acpower = 5;
            var sets = new List<PVSystemSettings>();
            foreach (var tilt in tilts) {
                foreach (var azimut in azimuts) {
                    var s = new PVSystemSettings(tilt, azimut, acpower, dcacratio, _logger);
                    sets.Add(s);
                }
            }

            var results = ParallelExecute(sets);

            //string dstpath = @"c:\work\t.csv";
            var dstsumspath = @"c:\work\sums.dcac." + dcacratio.ToString("F1") + ".csv";
            var srs = new List<SeasonResults>();
            foreach (var result in results) {
                srs.Add(result.CalcWinterSum());
            }

            ExportResults(srs, dstsumspath);
        }

        [Fact]
        public void MergedRandomProfile()
        {
            Directory.SetCurrentDirectory(@"V:\Dropbox\BurgdorfStatistics\Sam");
            _logger.Info("SSC Version number = " + API.Version());
            _logger.Info("SSC bBuild Information = " + API.BuildInfo());
            Module.SetPrint(0);
            //info
            //get variable info
            //Info info = new Info(module);
            //while (info.Get())
            // {
            //   Console.Write(info.Name() + ": ");
            //  Log(info.Constraints() + ":" + info.Label() + ":" +info.DataType());
            //}
            var r = new Random(1);
            var sets = new List<PVSystemSettings>();
            for (var i = 0; i < 100; i++) {
                sets.Add(new PVSystemSettings(r.Next(90), r.Next(360), 5, 1.2f, _logger));
            }

            var results = ParallelExecute(sets);
            var srs = new List<SeasonResults>();
            foreach (var result in results) {
                srs.Add(result.CalcWinterSum());
            }

            var mergedResults = MergeResults(srs);
            //string dstpath = @"c:\work\t.csv";
            const string dstsumspath = @"c:\work\sums_random2.csv";

            ExportResults(mergedResults, dstsumspath);
        }

        [NotNull]
        private List<SeasonResults> MergeResults([NotNull] List<SeasonResults> results)
        {
            var r = new SeasonResults(0, 0, 0);
            double sumtilt = 0;
            double sumazi = 0;
            foreach (var result in results) {
                sumtilt += result.Tilt;
                sumazi += result.Azimut;
                foreach (var pair in result.SeasonSums) {
                    r.SeasonSums[pair.Key].Noon += pair.Value.Noon / result.TotalPower;
                    r.SeasonSums[pair.Key].RestOfDay += pair.Value.RestOfDay / result.TotalPower;
                    for (var i = 0; i < 1440; i++) {
                        r.SeasonSums[pair.Key].WinterValues[i] += pair.Value.WinterValues[i] / result.TotalPower;
                    }
                }
            }

            r.Azimut = sumazi / results.Count;
            r.Tilt = sumtilt / results.Count;
            var sr = new List<SeasonResults> {
                r
            };
            return sr;
        }

        private static void ExportResults([NotNull] List<SeasonResults> results, [NotNull] string dstsumspath)
        {
//StreamWriter swAC = new StreamWriter(dstpath)
            var header1 = "";
            var header2 = "";
            var header3 = "";
            foreach (var result in results) {
                header1 += "'" + result.Azimut + "/" + result.Tilt + ";";
                header2 += result.Azimut + ";";
                header3 += result.Tilt + ";";
            }

            var swSums = new StreamWriter(dstsumspath);
            swSums.WriteLine("Azimut / Tilt;" + header1);
            swSums.WriteLine("Azimut;" + header2);
            swSums.WriteLine("Tilt;" + header3);


            var sumline = new StringBuilder();
            sumline.Append("YearlySum;");
            var stuffToWrite = new string[results.Count + 1, 1470 * 4];

            var hrow = 1;
            stuffToWrite[0, hrow++] = "Winter Sum";
            stuffToWrite[0, hrow++] = "Spring Sum";
            stuffToWrite[0, hrow++] = "Summer Sum";
            stuffToWrite[0, hrow++] = "Autumn Sum";
            stuffToWrite[0, hrow++] = "";
            stuffToWrite[0, hrow++] = "Winter Noon Sum";
            stuffToWrite[0, hrow++] = "Winter Rest Sum";
            stuffToWrite[0, hrow++] = "Spring Noon Sum";
            stuffToWrite[0, hrow++] = "Spring Rest Sum";
            stuffToWrite[0, hrow++] = "Summer Noon Sum";
            stuffToWrite[0, hrow++] = "Summer Rest Sum";
            stuffToWrite[0, hrow++] = "Autumn Noon Sum";
            stuffToWrite[0, hrow++] = "Autumn Rest Sum";
            foreach (var season in Enum.GetValues(typeof(Season))) {
                stuffToWrite[0, hrow++] = "";
                for (var i = 0; i < 1440; i++) {
                    stuffToWrite[0, hrow++] = season.ToString() + i;
                }
            }

            var column = 1;

            foreach (var sr in results) {
                var row = 1;
                stuffToWrite[column, row++] = (sr.SeasonSums[Season.Winter].Noon + sr.SeasonSums[Season.Winter].RestOfDay).ToString("F2");
                stuffToWrite[column, row++] = (sr.SeasonSums[Season.Spring].Noon + sr.SeasonSums[Season.Spring].RestOfDay).ToString("F2");
                stuffToWrite[column, row++] = (sr.SeasonSums[Season.Summer].Noon + sr.SeasonSums[Season.Summer].RestOfDay).ToString("F2");
                stuffToWrite[column, row++] = (sr.SeasonSums[Season.Autumn].Noon + sr.SeasonSums[Season.Autumn].RestOfDay).ToString("F2");
                stuffToWrite[column, row++] = "";
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Winter].Noon.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Winter].RestOfDay.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Spring].Noon.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Spring].RestOfDay.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Summer].Noon.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Summer].RestOfDay.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Autumn].Noon.ToString("F2");
                stuffToWrite[column, row++] = sr.SeasonSums[Season.Autumn].RestOfDay.ToString("F2");
                foreach (Season season in Enum.GetValues(typeof(Season))) {
                    stuffToWrite[column, row++] = "";
                    for (var i = 0; i < 1440; i++) {
                        stuffToWrite[column, row++] = sr.SeasonSums[season].WinterValues[i].ToString("F4");
                    }
                }

                column++;
            }

            for (var row = 0; row < stuffToWrite.GetLength(1); row++) {
                var line = new StringBuilder();
                for (var col = 0; col < stuffToWrite.GetLength(0); col++) {
                    line.Append(stuffToWrite[col, row]).Append(";");
                }

                swSums.WriteLine(line);
            }

            swSums.Close();
        }

        


        private class SubSums {
            public SubSums()
            {
                WinterValues = new List<double>();
                ValueCount = new List<int>();
                for (var i = 0; i < 1440; i++) {
                    WinterValues.Add(0);
                    ValueCount.Add(0);
                }
            }

            public double Noon { get; set; }
            public double RestOfDay { get; set; }
            public List<double> WinterValues { get; }
            public List<int> ValueCount { get; }


            public void AddValue(int minute, double value, bool isNoon)
            {
                WinterValues[minute] += value;
                ValueCount[minute]++;
                if (isNoon) {
                    Noon += value;
                }
                else {
                    RestOfDay += value;
                }
            }
        }

        public enum Season {
            Winter,
            Spring,
            Summer,
            Autumn
        }

        private class SeasonResults {
            public SeasonResults(double tilt, double azimut, double totalPower)
            {
                Tilt = tilt;
                Azimut = azimut;
                TotalPower = totalPower;
                var seasons = Enum.GetValues(typeof(Season));
                foreach (Season season in seasons) {
                    SeasonSums.Add(season, new SubSums());
                }
            }

            public Dictionary<Season, SubSums> SeasonSums { get; } = new Dictionary<Season, SubSums>();
            public double Tilt { get; set; }
            public double Azimut { get; set; }
            public double TotalPower { get; }

            public double ControlsumAndFixValues()
            {
                double d = 0;
                foreach (var season in SeasonSums.Values) {
                    d += season.Noon + season.RestOfDay;
                    season.Noon /= 60000;
                    season.RestOfDay /= 60000;
                    for (var i = 0; i < season.WinterValues.Count; i++) {
                        season.WinterValues[i] = season.WinterValues[i] / season.ValueCount[i];
                    }
                }

                return d;
            }
        }

        private class Results {
            public Results(double tilt, double azimut, double maxPower)
            {
                Tilt = tilt;
                Azimut = azimut;
                MaxPower = maxPower;
            }

            public double AnnualEnergy { get; set; }
            public double CapacityFactor { get; set; }

            public double KwhPerKW { get; set; }

            //public List<float> Tamb { get; set; }
            public List<float> AcPower { get; set; }
            public double Tilt { get; set; }
            public double Azimut { get; set; }

            public double MaxPower { get; set; }

            [NotNull]
            public SeasonResults CalcWinterSum()
            {
                var startOfSpring = new DateTime(2050, 3, 20);
                var startOfSummer = new DateTime(2050, 6, 21);
                var startOfAutum = new DateTime(2050, 9, 23);
                var startOfwinter = new DateTime(2050, 12, 12);
                var endTime = new DateTime(2051, 1, 1);

                var sr = new SeasonResults(Tilt, Azimut, MaxPower);
                var i = 0;
                var dt = new DateTime(2050, 1, 1);

                while (dt < endTime) {
                    Season season;
                    if (dt < startOfSpring) {
                        season = Season.Winter;
                    }
                    else if (dt < startOfSummer) {
                        season = Season.Spring;
                    }
                    else if (dt < startOfAutum) {
                        season = Season.Summer;
                    }
                    else if (dt < startOfwinter) {
                        season = Season.Autumn;
                    }
                    else {
                        season = Season.Winter;
                    }

                    var isNoon = false;
                    if (dt.Hour > 11 && dt.Hour < 13) {
                        isNoon = true;
                    }

                    var minuteOfDay = dt.Hour * 60 + dt.Minute;
                    sr.SeasonSums[season].AddValue(minuteOfDay, AcPower[i], isNoon);
                    i++;
                    dt = dt.AddMinutes(1);
                }

                var controlSum = sr.ControlsumAndFixValues();
                double totalsum = AcPower.Sum();
                if (Math.Abs(controlSum - totalsum) > 100) {
                    throw new Exception("Controlsum doesn't fit");
                }

                return sr;
            }
        }

        [NotNull]
        private static Results ExecuteCalculation([NotNull] Module module, [NotNull] Data data, double tilt, double azimut, Logging.Logger logger)
        {
            if (!module.Exec(data)) {
                var idx = 0;
                while (module.Log(idx, out string msg, out int type, out float time))
                {
                    var stype = "NOTICE";
                    if (type == API.WARNING)
                    {
                        stype = "WARNING";
                    }
                    else if (type == API.ERROR)
                    {
                        stype = "ERROR";
                    }

                    logger.Info("[" + stype + " at time : " + time + "]: " + msg);
                    idx++;
                }

                throw new Exception("something went wrong");
            }

            var r = new Results(tilt, azimut, 5) {
                Tilt = tilt,
                Azimut = azimut,
                AnnualEnergy = data.GetNumber("annual_energy"),
                CapacityFactor = data.GetNumber("capacity_factor"),
                KwhPerKW = data.GetNumber("kwh_per_kw"),
                AcPower = data.GetArrayAsList("ac")
            };
            return r;
        }

     
    }
}*/