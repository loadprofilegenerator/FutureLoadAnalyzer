using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using MathNet.Numerics.Statistics;
using Visualizer;
using Visualizer.OSM;

namespace FutureLoadAnalyzerLib._00_Import {
    // ReSharper disable once InconsistentNaming
    public class A07_GwrImporter : RunableWithBenchmark {
        public A07_GwrImporter([NotNull] ServiceRepository services) : base(nameof(A07_GwrImporter), Stage.Raw, 7, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            string fn = CombineForRaw("GWRData.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(fn, 1, "A1", "AW4000", out var _);
            var headerToColumnDict = new Dictionary<string, int>();
            for (var i = 0; i < arr.GetLength(1) - 1; i++) {
                var o = arr[1, i];
                if (o == null) {
                    throw new Exception("was null");
                }

                headerToColumnDict.Add(o.ToString(), i);
            }

            var db = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            db.RecreateTable<GwrData>();
            db.BeginTransaction();
            for (var row = 2; row < arr.GetLength(0); row++) {
                var a = new GwrData();
                if (arr[row, headerToColumnDict["EGID"]] == null) {
                    continue;
                }

                TransferFields(arr, headerToColumnDict, row, a);
                db.Save(a);
            }

            db.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var gwr = dbRaw.Fetch<GwrData>();
            Debug("Loaded gwr data: " + gwr.Count);
            var ebd = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            Debug("Loaded energiebedarfsdaten " + ebd.Count);
            MakeMap();
            MakeGWRArea();
            MakeAnzahlGeschosse();
            MakeAnzahlWohnungen();
            Debug("Testmap written");

            void MakeAnzahlWohnungen()
            {
                var whg = gwr.Select(x => x.AnzahlWohnungen_GANZWHG).Where(x => x != null).ToList()
                    // ReSharper disable once PossibleInvalidOperationException
                    .ConvertAll(x => (double)x).ToList();
                const string baseName = "GWRVisual_GanzWhg";
                const int bucketSize = 1;
                MakeOneAnalysis(whg, bucketSize, baseName, slice);
            }

            void MakeAnzahlGeschosse()
            {
                var whg = gwr.Select(x => x.AnzahlGeschosse_GASTW).Where(x => x != null).ToList()
                    // ReSharper disable once PossibleInvalidOperationException
                    .ConvertAll(x => (double)x).ToList();
                const string baseName = "GWRVisual_AnzahlGeschosse";
                const int bucketSize = 1;
                MakeOneAnalysis(whg, bucketSize, baseName, slice);
            }

            void MakeGWRArea()
            {
                var areas = gwr.Select(x => x.Gebaeudeflaeche_GAREA).Where(x => x != null).ToList().ConvertAll(x => {
                    if (x == null) {
                        throw new FlaException("x was null");
                    }

                    return (double)x;
                }).ToList();
                const string baseName = "GWRVisual_GArea";
                const int bucketSize = 50;
                MakeOneAnalysis(areas, bucketSize, baseName, slice);
            }

            void MakeMap()
            {
                var points = new List<MapPoint>();
                var missingEntries = 0;
                foreach (var data in gwr) {
                    var ebdo = ebd.FirstOrDefault(x => x.egid == (data.EidgGebaeudeidentifikator_EGID ?? -1));
                    var rad = 10;
                    if (ebdo != null) {
                        rad = (int)Math.Sqrt((int)ebdo.garea);
                    }
                    else {
                        Trace("No area entry from energiebedarfsdaten #" + missingEntries++);
                    }

                    if (data.XKoordinate_GKODX != null) {
                        // ReSharper disable once PossibleInvalidOperationException
                        points.Add(new MapPoint(data.XKoordinate_GKODX.Value, data.YKoordinate_GKODY.Value, data.AnzahlWohnungen_GANZWHG ?? 1, rad));
                    }
                }

                string fullName = MakeAndRegisterFullFilename("HousesAndApartmentCounts.svg", Constants.PresentSlice);
                Services.PlotMaker.MakeMapDrawer(fullName, fullName, points, new List<MapLegendEntry>());
            }
        }

        private void MakeOneAnalysis([NotNull] List<double> areas, int bucketSize, [NotNull] string baseName, [NotNull] ScenarioSliceParameters slice)
        {
            {
                var maxArea = areas.Max();
                var histogram = new Histogram();
                for (var i = 0; i < bucketSize * 10; i += bucketSize) {
                    histogram.AddBucket(new Bucket(i, i + bucketSize));
                }

                if (maxArea > bucketSize * 10) {
                    histogram.AddBucket(new Bucket(bucketSize * 10, maxArea));
                }

                histogram.AddData(areas);
                string histogramfilename = MakeAndRegisterFullFilename(baseName + "_Histogram.png", slice);
                var bs = BarSeriesEntry.MakeBarSeriesEntry(histogram, out var colNames);
                var bss = new List<BarSeriesEntry> {
                    bs
                };
                Services.PlotMaker.MakeBarChart(histogramfilename, "Anzahl von Haushalten mit Fläche in diesem Bereich", bss, colNames);
            }
            {
                string dstFileName2 = MakeAndRegisterFullFilename(baseName + "_SortedAreas.png", slice);
                var lse = new LineSeriesEntry("Sorted");
                var sorted = areas.ToList();
                sorted.Sort();
                for (var i = 0; i < sorted.Count; i++) {
                    lse.Values.Add(new Point(i, sorted[i]));
                }

                var lss = new List<LineSeriesEntry> {
                    lse
                };
                Services.PlotMaker.MakeLineChart(dstFileName2, baseName + "_SortedAreas", lss, new List<AnnotationEntry>());
            }
            {
                string dstFileName2 = MakeAndRegisterFullFilename(baseName + "_Kumulativ.png", slice);
                var lse = new LineSeriesEntry("Kumulativ");
                var sorted = areas.ToList();
                sorted.Sort();
                double tempSum = 0;
                for (var i = 0; i < sorted.Count; i++) {
                    tempSum += sorted[i];
                    lse.Values.Add(new Point(i, tempSum));
                }

                var lss = new List<LineSeriesEntry> {
                    lse
                };
                Services.PlotMaker.MakeLineChart(dstFileName2, baseName + "_Kumulativ", lss, new List<AnnotationEntry>());
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void TransferFields([NotNull] [ItemNotNull] object[,] arr,
                                           [NotNull] Dictionary<string, int> hdict,
                                           int row,
                                           [NotNull] GwrData a)
        {
            a.EidgGebaeudeidentifikator_EGID = Convert.ToInt32(arr[row, hdict["EGID"]]);
            a.EidgBauprojektidentifikator_EPROID = Helpers.GetInt(arr[row, hdict["EPROID"]]);
            a.ErhebungsstelleBaustatistik_GESTNR = Helpers.GetInt(arr[row, hdict["GESTNR"]]);
            a.BauprojektIdLiefersystem_GBABID = (string)arr[row, hdict["GBABID"]];
            a.GebaeudeIDLiefersystem_GBAGID = (string)arr[row, hdict["GBAGID"]];
            a.BFSGemeindenummer_GDENR = Helpers.GetInt(arr[row, hdict["GDENR"]]);
            a.EidgGrundstuecksidentifikator_GEGRID = (string)arr[row, hdict["GEGRID"]];
            a.Grundbuchkreisnummer_GGBKR = Helpers.GetInt(arr[row, hdict["GGBKR"]]);
            a.Parzellennummer_GPARZ = Helpers.GetInt(arr[row, hdict["GPARZ"]]);
            a.AmtlicheGebaeudenummer_GEBNR = Helpers.GetInt(arr[row, hdict["GEBNR"]]);
            a.NamedesGebaeudes_GBEZ = (string)arr[row, hdict["GBEZ"]];
            a.AnzahlGebaeudeeingaenge_GANZDOM = Helpers.GetInt(arr[row, hdict["GANZDOM"]]);
            a.EKoordinate_GKODE = Helpers.GetDouble(arr[row, hdict["GKODE"]]);
            a.NKoordinate_GKODN = Helpers.GetDouble(arr[row, hdict["GKODN"]]);
            a.XKoordinate_GKODX = Helpers.GetDouble(arr[row, hdict["GKODX"]]);
            a.YKoordinate_GKODY = Helpers.GetDouble(arr[row, hdict["GKODY"]]);
            a.Koordinatenherkunft_GKSCE = Helpers.GetInt(arr[row, hdict["GKSCE*"]]);
            a.Lokalcode1_GLOC1 = Helpers.GetInt(arr[row, hdict["GLOC1"]]);
            a.Lokalcode2_GLOC2 = Helpers.GetInt(arr[row, hdict["GLOC2"]]);
            a.Lokalcode3_GLOC3 = Helpers.GetInt(arr[row, hdict["GLOC3"]]);
            a.Lokalcode4_GLOC4 = Helpers.GetInt(arr[row, hdict["GLOC4"]]);
            a.Gebaeudestatus_GSTAT = Helpers.GetInt(arr[row, hdict["GSTAT*"]]);
            a.Gebaeudekategorie_GKAT = Helpers.GetInt(arr[row, hdict["GKAT*"]]);
            a.Gebaeudeklasse_GKLAS = Helpers.GetInt(arr[row, hdict["GKLAS*"]]);
            a.Baujahr_GBAUJ = Helpers.GetInt(arr[row, hdict["GBAUJ"]]);
            a.Bauperiode_GBAUP = Helpers.GetInt(arr[row, hdict["GBAUP*"]]);
            a.Renovationsjahr_GRENJ = Helpers.GetInt(arr[row, hdict["GRENJ"]]);
            a.Renovationsperiode_GRENP = Helpers.GetInt(arr[row, hdict["GRENP*"]]);
            a.Abbruchjahr_GABBJ = Helpers.GetInt(arr[row, hdict["GABBJ"]]);
            a.Gebaeudeflaeche_GAREA = Helpers.GetInt(arr[row, hdict["GAREA"]]);
            a.AnzahlGeschosse_GASTW = Helpers.GetInt(arr[row, hdict["GASTW"]]);
            a.AnzahlseparateWohnraeume_GAZZI = Helpers.GetInt(arr[row, hdict["GAZZI"]]);
            a.AnzahlWohnungen_GANZWHG = Helpers.GetInt(arr[row, hdict["GANZWHG"]]);
            a.Heizungsart_GHEIZ = Helpers.GetInt(arr[row, hdict["GHEIZ*"]]);
            a.EnergietraegerderHeizung_GENHZ = Helpers.GetInt(arr[row, hdict["GENHZ*"]]);
            a.Warmwasserversorgung_GWWV = Helpers.GetInt(arr[row, hdict["GWWV**"]]);
            a.EnergietraegerfuerWarmwasser_GENWW = Helpers.GetInt(arr[row, hdict["GENWW*"]]);
            a.AnzahlEingangsrecords_GADOM = Helpers.GetInt(arr[row, hdict["GADOM"]]);
            a.AnzahlWohnungsrecords_GAWHG = Helpers.GetInt(arr[row, hdict["GAWHG"]]);
            a.PlausibilitaetsstatusderKoordinaten_GKPLAUS = Helpers.GetInt(arr[row, hdict["GKPLAUS"]]);
            a.StatusWohnungsbestandes_GWHGSTD = Helpers.GetInt(arr[row, hdict["GWHGSTD*"]]);
            a.VerifikationWohnungsbestand_GWHGVER = Helpers.GetInt(arr[row, hdict["GWHGVER**"]]);
            a.PlausibilitaetsstatusGebaeude_GPLAUS = Helpers.GetInt(arr[row, hdict["GPLAUS*"]]);
            a.Baumonat_GBAUM = (string)arr[row, hdict["GBAUM"]];
            a.Renovationsmonat_GRENM = Helpers.GetInt(arr[row, hdict["GRENM"]]);
            a.Abbruchmonat_GABBM = Helpers.GetInt(arr[row, hdict["GABBM"]]);
        }
    }
}