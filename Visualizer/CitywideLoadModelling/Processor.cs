using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BurgdorfStatistics._08_ProfileImporter;
using Data;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.Mapper;
using Xunit;

namespace BurgdorfStatistics.CitywideLoadModelling {
    public class Processor {
        [Fact]
        public void Run()
        {
            var logger = new Logging.Logger(null);
            var pm = new PlotMaker(new MapDrawer(logger), logger, null);
            const string targetDir = @"c:\work\simzukunft\Stadtprofile";
            //read profiles
            const string filename = @"U:\SimZukunft\StadtProfil\BKWLast_1h.csv";
            const string profilename = "01-bkwlast";
            var bkwRaw = ZZ_ProfileImportHelper.ReadCSV(filename, profilename);
            var bkwScaled = bkwRaw.ScaleToTargetSum(103_000_000, "BKW");
            VisualizeOneProfile(targetDir, bkwScaled, pm, false);
            const string filename2 = @"U:\SimZukunft\StadtProfil\PVEinspeisung_1h.csv";
            const string profilename2 = "01-pv";
            var evProfile = Profile.MakeConstantProfile(15_000_000, "EV", Profile.ProfileResolution.Hourly);
            var pv = ZZ_ProfileImportHelper.ReadCSV(filename2, profilename2);
            VisualizeOneProfile(targetDir, pv, pm, false);
            //scaling

            const string filename3 = @"U:\SimZukunft\StadtProfil\temperaturen.csv";
            const string profilename3 = "01-temperaturen";
            var temperaturen = ZZ_ProfileImportHelper.ReadCSV(filename3, profilename3);

            const string filename4 = @"U:\SimZukunft\StadtProfil\pv_einspeisung_pvgis_2016_35_süd.csv";
            const string profilename4 = "pvgis_süd_35";
            var pvgis35 = ZZ_ProfileImportHelper.ReadCSV(filename4, profilename4);

            const string filename5 = @"U:\SimZukunft\StadtProfil\pv_einspeisung_pvgis_2016_90_süd.csv";
            const string profilename5 = "pvgis_süd_90";
            var pvgis90 = ZZ_ProfileImportHelper.ReadCSV(filename5, profilename5);


            VisualizeOneProfile(targetDir, temperaturen, pm, false);

            var degreeDayPowerProfile = temperaturen.MakeDegreeDayPowerProfile("02-HeatingDegreedays", 15, 22_000_000);

            var coolingdegreeDayPowerProfile = temperaturen.MakeCoolingDegreeHours("02-CoolingDegreeHours", 20, 15_000_000);
            VisualizeOneProfile(targetDir, coolingdegreeDayPowerProfile, pm, false);
            var summedLoadProfile = bkwScaled.Add(evProfile, "with ev").Add(degreeDayPowerProfile, "with heating").Add(coolingdegreeDayPowerProfile, "with cooling");
            var bkwsum = summedLoadProfile.EnergySum();
            var scaledPv1 = pv.ScaleToTargetSum(bkwsum, "02-PV Scaled");

            var scaledPv35 = pvgis35.ScaleToTargetSum(bkwsum, "pvgis_süd_35_scaled");
            VisualizeOneProfile(targetDir, scaledPv35, pm, false);
            var scaledPv90 = pvgis90.ScaleToTargetSum(bkwsum, "pvgis_süd_90_scaled");
            VisualizeOneProfile(targetDir, scaledPv90, pm, false);
            VisualizeOneProfile(targetDir, degreeDayPowerProfile, pm, false);
            const string targetdir1 = @"c:\work\simzukunft\Stadtprofile\measured";
            AnalyzePVProfile(bkwScaled, scaledPv1, degreeDayPowerProfile, evProfile, coolingdegreeDayPowerProfile, targetdir1, pm, summedLoadProfile);

            const string targetdir2 = @"c:\work\simzukunft\Stadtprofile\35süd";
            AnalyzePVProfile(bkwScaled, scaledPv35, degreeDayPowerProfile, evProfile, coolingdegreeDayPowerProfile, targetdir2, pm, summedLoadProfile);
            const string targetdir3 = @"c:\work\simzukunft\Stadtprofile\90süd";
            AnalyzePVProfile(bkwScaled, scaledPv90, degreeDayPowerProfile, evProfile, coolingdegreeDayPowerProfile, targetdir3, pm, summedLoadProfile);
            //var withStorage50KWh = residualAfterPv.IntegrateStorage(50 * 4000, "06-50kwhStorage", out var storageValues50Kwh);
            //VisualizeOneProfile(withStorage50KWh, pm, true);
            //VisualizeOneProfile(storageValues50Kwh, pm, false);
            //var onlypositiveBattery50Kwh = withStorage50KWh.GetOnlyPositive("06-50kwhBatterly only positive");
            //VisualizeOneProfile(onlypositiveBattery50Kwh, pm, false);

            //level target for 10 kWh
            //var targetLoad = FindMinimumTargetLoad(summedLoadProfile, residualAfterPv,10);
            //var leveledProfile =residualAfterPv.IntegrateStorageWithAdjustedCurrentValue(20 * 4000,"07-20kwhStoragePredictive-target-" + targetLoad,out var _, targetLoad);
            //VisualizeOneProfile(leveledProfile, pm, true);
            //VisualizeOneProfile(leveledProfile.GetOnlyPositive("07-20kwh_predicitive_onlypositive"), pm, true);

            //look at minimum power draw over different storage sizes
            //Profile minimumTargetLoads = new Profile("08-MinimumTargetLoads");
            //double maxLoad = summedLoadProfile.Values.Max();
            //for (int i = 0; i < 3000; i+=10) {
            //var targetLoad1 = FindMinimumTargetLoad(summedLoadProfile, residualAfterPv,i);
            //double relativeLoad = targetLoad1 / maxLoad;
            //minimumTargetLoads.Values.Add(relativeLoad);
            //}
            //VisualizeOneProfile(minimumTargetLoads, pm, true);

            pm.Finish();
        }

        private void AnalyzePVProfile([NotNull] Profile bkwScaled, [NotNull] Profile pvProfile, [NotNull] Profile degreeDayPowerProfile, [NotNull] Profile evProfile,
                                      [NotNull] Profile coolingdegreeDayPowerProfile, [NotNull] string targetDir, [NotNull] PlotMaker pm, [NotNull] Profile summedLoadProfile)
        {
            var stackedProfiles = new List<Profile> {
                bkwScaled,
                pvProfile.MultiplyWith(-1, "PV"),
                degreeDayPowerProfile,
                evProfile,
                coolingdegreeDayPowerProfile
            };
            var residualAfterPv = summedLoadProfile.MinusProfile(pvProfile, "03-ResidualAfterPV");
            VisualizeStackedProfiles(targetDir, stackedProfiles, pm, "0a-FullCityStack", residualAfterPv);

            VisualizeOneProfile(targetDir, residualAfterPv, pm, true);
            residualAfterPv.Name += " full";
            VisualizeOneProfile(targetDir, residualAfterPv, pm, false);
            //pm.Finish();return;
            var onlyNegative = residualAfterPv.GetOnlyNegative("03-Residual after pv only negative");
            VisualizeOneProfile(targetDir, onlyNegative, pm, false);
            var onlypositive = residualAfterPv.GetOnlyPositive("03-Residual after pv only positive");
            VisualizeOneProfile(targetDir, onlypositive, pm, false);
            const int batterySize = 400000;
            var withStorage = residualAfterPv.IntegrateStorage(batterySize, "04-" + batterySize + "kwhStorage", out var storageValues);
            VisualizeOneProfile(targetDir, withStorage, pm, false);
            withStorage.Name += "-limited";
            VisualizeOneProfile(targetDir, withStorage, pm, true);
            VisualizeOneProfile(targetDir, storageValues, pm, false);
            var onlypositiveBattery = withStorage.GetOnlyPositive("04-" + batterySize + "kwhBattery only positive");
            VisualizeOneProfile(targetDir, onlypositiveBattery, pm, false);
        }
        /*
        private static double FindMinimumTargetLoad([NotNull] Profile bkw, [NotNull] Profile residualAfterPv, double storageSize)
        {
            if (Math.Abs(storageSize) < 0.00001) {
                return bkw.Values.Max();
            }
            double targetLoad =0;
            double peakLoad = Double.MaxValue;
            while (peakLoad > targetLoad) {
                var withStorage10KWhPredictive =
                    residualAfterPv.IntegrateStorageWithAdjustedCurrentValue(storageSize * 4000,
                        "10kwhStoragePredictive-target-" + targetLoad,
                        out var _, targetLoad);
                peakLoad = withStorage10KWhPredictive.Values.Max();
                if (peakLoad > targetLoad) {
                    targetLoad += 50;
                }
                //logger.Info("trying targete load of " + targetLoad);
            }
            return targetLoad;
        }*/

        private void VisualizeStackedProfiles([NotNull] string targetDir, [ItemNotNull] [NotNull] List<Profile> ps, [NotNull] PlotMaker pm, [NotNull] string name, [NotNull] Profile residual)
        {
            if (!Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
                Thread.Sleep(500);
            }

            var filename = Path.Combine(targetDir, name + ".png");
            var filenameCsv = Path.Combine(targetDir, name + ".csv");
            var filenameHourlyCsv = Path.Combine(targetDir, name + ".hourly.csv");
            var allLs = new List<LineSeriesEntry>();
            double min = 0;
            var allBars = new List<BarSeriesEntry>();
            foreach (var profile in ps) {
                var ls = profile.GetLineSeriesEntry();
                allLs.Add(ls);
                if (profile.Values.Min() < min) {
                    min = profile.Values.Min();
                }

                allBars.Add(profile.MakeDailyAverages().GetBarSeries());
            }

            pm.MakeLineChart(filename, name, allLs, new List<PlotMaker.AnnotationEntry>(), min);

            pm.MakeBarChart(filename + ".bars.png", name, allBars, new List<string>());
            {
                using (var sw = new StreamWriter(filenameCsv)) {
                    var sb1 = new StringBuilder();
                    foreach (var profile in ps) {
                        sb1.Append(profile.Name).Append(";");
                    }

                    sw.WriteLine(sb1);
                    var dailyProfiles = new List<Profile>();
                    foreach (var profile in ps) {
                        dailyProfiles.Add(profile.MakeDailyAverages());
                    }

                    for (var i = 0; i < 365; i++) {
                        var sb = new StringBuilder();
                        foreach (var profile in dailyProfiles) {
                            sb.Append(profile.Values[i]).Append(";");
                        }

                        sw.WriteLine(sb);
                    }

                    sw.Close();
                }
            }
            {
                using (var sw = new StreamWriter(filenameHourlyCsv)) {
                    var sb1 = new StringBuilder();
                    ps.Add(residual);
                    foreach (var profile in ps) {
                        sb1.Append(profile.Name).Append(";");
                    }

                    sw.WriteLine(sb1);
                    for (var i = 0; i < 8760; i++) {
                        var sb = new StringBuilder();
                        foreach (var profile in ps) {
                            sb.Append(profile.Values[i]).Append(";");
                        }

                        sw.WriteLine(sb);
                    }

                    sw.Close();
                }
            }
        }

        private void VisualizeOneProfile([NotNull] string targetDir, [NotNull] Profile p, [NotNull] PlotMaker pm, bool limitToZero)
        {
            if (!Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
                Thread.Sleep(500);
            }

            var filename = Path.Combine(targetDir, p.Name + ".png");
            var filenameCsv = Path.Combine(targetDir, p.Name + ".csv");
            var ls = p.GetLineSeriesEntry();
            var allLs = new List<LineSeriesEntry> {
                ls
            };
            pm.MakeLineChart(filename, p.Name, allLs, new List<PlotMaker.AnnotationEntry>(), limitToZero ? 0 : p.Values.Min());

            if (p.Values.Count < 8700) {
                using (var sw1 = new StreamWriter(filenameCsv)) {
                    for (var i = 0; i < p.Values.Count; i++) {
                        sw1.WriteLine(p.Values[i]);
                    }

                    sw1.Close();
                    return;
                }
            }

            var oneDayProfile = p.MakeDailyAverages();
            var bs = oneDayProfile.GetBarSeries();
            var lbs = new List<BarSeriesEntry> {
                bs
            };
            var times = new List<string>();
            for (var i = 0; i < oneDayProfile.Values.Count; i++) {
                if (i % 30 == 0) {
                    times.Add(i.ToString());
                }
                else {
                    times.Add("");
                }
            }

            pm.MakeBarChart(filename + ".bar.png", p.Name, lbs, times);

            var negatives = p.GetOnlyNegative("load").MakeDailyAverages().GetBarSeries();
            var positives = p.GetOnlyPositive("feedin").MakeDailyAverages().GetBarSeries();

            var lbs2 = new List<BarSeriesEntry> {
                negatives,
                positives
            };
            pm.MakeBarChart(filename + ".barSplit.png", p.Name, lbs2, times);

            using (var sw = new StreamWriter(filenameCsv)) {
                foreach (var value in p.Values) {
                    sw.WriteLine(value);
                }

                sw.Close();
            }
        }
    }
}