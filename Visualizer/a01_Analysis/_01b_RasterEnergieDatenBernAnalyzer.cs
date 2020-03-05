using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Data;
using Data.DataModel.Src;
using JetBrains.Annotations;
using NPoco;
using Visualizer;
using Visualizer.Mapper;

namespace BurgdorfStatistics.a01_Analysis {
    // ReSharper disable once InconsistentNaming
    internal class _01b_RasterEnergieDatenBernAnalyzer {
        // ReSharper disable once FunctionComplexityOverflow
        public void Run()
        {
            var logger = new Logging.Logger(null);
            using (var db = new Database("Data Source=v:\\Test.sqlite", DatabaseType.SQLite, System.Data.SQLite.SQLiteFactory.Instance)) {
                var ebd = db.Fetch<RasterDatenEnergiebedarfKanton>();
                var dict = GetLongNames();
                var pm = new PlotMaker(new MapDrawer(logger), logger, null);
                const string path = "v:\\plots\\";
                //anzahl gebäude
                var bses = new List<BarSeriesEntry>();
                var colNames = new List<string>();
                var col = 0;
                colNames.Add("Summe");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_gwr)], ebd.Sum(x => x.wg_gwr), col));
                col++;
                colNames.Add("GEAK");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_geak)], ebd.Sum(x => x.wg_geak), col));
                col++;
                colNames.Add("Gebäude nach Typ");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1010)], ebd.Sum(x => x.wg_1010), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1021)], ebd.Sum(x => x.wg_1021), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1025)], ebd.Sum(x => x.wg_1025), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1030)], ebd.Sum(x => x.wg_1030), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1040)], ebd.Sum(x => x.wg_1040), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1060)], ebd.Sum(x => x.wg_1060), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.wg_1080)], ebd.Sum(x => x.wg_1080), col));
                pm.MakeBarChart(Path.Combine(path, "Gebäudeanzahl" + ".png"), "Gebäude-Anzahl", bses, colNames);

                //wärme/energiebedarfe
                bses = new List<BarSeriesEntry>();
                colNames = new List<string>();
                col = 0;
                colNames.Add("Wärmebedarf für \nHeizen und\nWarmwasser ");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.whzww)], ebd.Sum(x => x.whzww), col));
                col++;
                colNames.Add("Wärmebedarf \naufgeteilt");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.whz)], ebd.Sum(x => x.whz), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.www)], ebd.Sum(x => x.www), col));
                col++;
                colNames.Add("Energiebedarf \nGesamt");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww)], ebd.Sum(x => x.ehzww), col));
                col++;
                colNames.Add("Energiebedarf \nWarmwasser und\nHeizen");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz)], ebd.Sum(x => x.ehz), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww)], ebd.Sum(x => x.eww), col));
                col++;
                colNames.Add("Energiebedarf \nHeizen \naufteilt");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_ol)], ebd.Sum(x => x.ehz_ol), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_gz)], ebd.Sum(x => x.ehz_gz), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_ho)], ebd.Sum(x => x.ehz_ho), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_el)], ebd.Sum(x => x.ehz_el), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_ko)], ebd.Sum(x => x.ehz_ko), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_wp)], ebd.Sum(x => x.ehz_wp), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_so)], ebd.Sum(x => x.ehz_so), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_fw)], ebd.Sum(x => x.ehz_fw), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_a)], ebd.Sum(x => x.ehz_a), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehz_u)], ebd.Sum(x => x.ehz_u), col));
                col++;
                colNames.Add("Energiebedarf \nWarmwasser \naufteilt");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_ol)], ebd.Sum(x => x.eww_ol), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_gz)], ebd.Sum(x => x.eww_gz), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_ho)], ebd.Sum(x => x.eww_ho), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_el)], ebd.Sum(x => x.eww_el), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_ko)], ebd.Sum(x => x.eww_ko), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_wp)], ebd.Sum(x => x.eww_wp), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_so)], ebd.Sum(x => x.eww_so), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_fw)], ebd.Sum(x => x.eww_fw), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_a)], ebd.Sum(x => x.eww_a), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.eww_u)], ebd.Sum(x => x.eww_u), col));
                col++;
                colNames.Add("Energiebedarf \nGesamt\nnach Haustyp");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1010)], ebd.Sum(x => x.ehzww_1010), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1021)], ebd.Sum(x => x.ehzww_1021), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1025)], ebd.Sum(x => x.ehzww_1025), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1030)], ebd.Sum(x => x.ehzww_1030), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1040)], ebd.Sum(x => x.ehzww_1040), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1060)], ebd.Sum(x => x.ehzww_1060), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_1080)], ebd.Sum(x => x.ehzww_1080), col));
                col++;
                colNames.Add("Energiebedarf \nGesamt\nnach Baujahr");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8011)], ebd.Sum(x => x.ehzww_8011), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8012)], ebd.Sum(x => x.ehzww_8012), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8013)], ebd.Sum(x => x.ehzww_8013), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8014)], ebd.Sum(x => x.ehzww_8014), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8015)], ebd.Sum(x => x.ehzww_8015), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8016)], ebd.Sum(x => x.ehzww_8016), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8017)], ebd.Sum(x => x.ehzww_8017), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8018)], ebd.Sum(x => x.ehzww_8018), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8019)], ebd.Sum(x => x.ehzww_8019), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8020)], ebd.Sum(x => x.ehzww_8020), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8021)], ebd.Sum(x => x.ehzww_8021), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8022)], ebd.Sum(x => x.ehzww_8022), col));
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio(dict[nameof(RasterDatenEnergiebedarfKanton.ehzww_8023)], ebd.Sum(x => x.ehzww_8023), col));
                col++;
                colNames.Add("Energiebedarf \nGesamt\nRichtplan 2012");
                bses.Add(BarSeriesEntry.MakeBarSeriesEntryDividedBy1Mio("Richtplan 2012 Total", 111131000, col));

                pm.MakeBarChart(Path.Combine(path, "WärmeBedarfe" + ".png"), "Wärme", bses, colNames);


                pm.Finish();
            }
        }

        [NotNull]
        public static Dictionary<string, string> GetLongNames()
        {
            var dict = new Dictionary<string, string>();

            var props = typeof(RasterDatenEnergiebedarfKanton).GetProperties();
            foreach (var prop in props) {
                var attrs = prop.GetCustomAttributes(true);
                foreach (var attr in attrs) {
                    var authAttr = attr as LongNameAttribute;
                    if (authAttr != null) {
                        var propName = prop.Name;
                        var auth = authAttr.LongName;

                        dict.Add(propName, auth);
                    }
                }
            }

            return dict;
        }
    }
}