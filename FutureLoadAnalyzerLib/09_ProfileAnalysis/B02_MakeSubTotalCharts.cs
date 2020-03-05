using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    // ReSharper disable once InconsistentNaming
    public class B02_MakeSubTotalCharts : RunableForSingleSliceWithBenchmark {
        public B02_MakeSubTotalCharts([NotNull] ServiceRepository services) : base(nameof(B02_MakeSubTotalCharts),
            Stage.ProfileAnalysis,
            202,
            services,
            false)
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
        }

        protected override void RunChartMaking(ScenarioSliceParameters slice)
        {
            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saArchive = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
            var archiveEntries = saArchive.LoadAllOrMatching();
            var dbSrcProfiles = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var bkwRaw = dbSrcProfiles.Fetch<BkwProfile>();
            MakePlotlyLineCharts();
            MakePlotlyTrafostationBoxPlots();

            MakeStackedTrafokreise();
            MakeEnergyProTrafokreis();

            void MakeEnergyProTrafokreis()
            {
                var trafoKreise = archiveEntries.Where(x => x.Key.SumType == SumType.ByTrafokreis).ToList();
                BarSeriesEntry bseEnergy = new BarSeriesEntry("Energy [GWh]");
                BarSeriesEntry bseAveragePower = new BarSeriesEntry("Average Power [kW]");
                List<string> labels = new List<string>();
                foreach (var tk in trafoKreise) {
                    double energySum = tk.Profile.EnergySum();
                    bseEnergy.Values.Add(energySum / 1_000_000);
                    bseAveragePower.Values.Add(energySum / 8760);
                    labels.Add(tk.Key.Trafokreis);
                }

                var bses = new List<BarSeriesEntry>();
                bses.Add(bseEnergy);
                var fn = MakeAndRegisterFullFilename("EnergyPerTrafokreis.png", slice);
                Services.PlotMaker.MakeBarChart(fn, bseEnergy.Name, bses, labels);

                var bsesPower = new List<BarSeriesEntry>();
                bsesPower.Add(bseAveragePower);
                var fn2 = MakeAndRegisterFullFilename("DurchschnittsleistungPerTrafokreis.png", slice);
                Services.PlotMaker.MakeBarChart(fn2, bseAveragePower.Name, bsesPower, labels);
            }

            void MakePlotlyTrafostationBoxPlots()
            {
                var trafoKreise = archiveEntries.Where(x => x.Key.SumType == SumType.ByTrafokreis).ToList();
                var fn2 = MakeAndRegisterFullFilename("Boxplots.html", slice);
                List<BoxplotTrace> bpts = new List<BoxplotTrace>();
                int height = 100;
                foreach (var entry in trafoKreise) {
                    BoxplotTrace bpt = new BoxplotTrace(entry.Key.Trafokreis, entry.Profile.ConvertFromEnergyToPower().Get5BoxPlotValues());
                    bpts.Add(bpt);
                    height += 25;
                }

                var layout = new PlotlyLayout {
                    Title = "Leistungen pro Trafostation",
                    Height = height,
                    Margin = new Margin {
                        Left = 200
                    }
                };
                FlaPlotlyPlot fpp = new FlaPlotlyPlot();
                fpp.RenderToFile(bpts, layout, null, fn2);
            }

            void MakePlotlyLineCharts()
            {
                var providers = archiveEntries.Where(x => x.Key.SumType == SumType.ByProvider).ToList();
                List<double> timestep = new List<double>();
                for (int i = 0; i < 8760 * 24 * 4; i++) {
                    timestep.Add(i);
                }

                FlaPlotlyPlot fpp = new FlaPlotlyPlot();
                foreach (var entry in providers) {
                    var fn2 = MakeAndRegisterFullFilename("LineChartsPerProvider." + "." + entry.GenerationOrLoad + "." + entry.Key.ProviderType +
                                                          ".html",
                        slice);
                    LineplotTrace bpt = new LineplotTrace(entry.Key.Trafokreis, timestep, entry.Profile.Values.ToList());
                    List<LineplotTrace> lpt = new List<LineplotTrace>();
                    lpt.Add(bpt);
                    fpp.RenderToFile(lpt, null, null, fn2);
                }
            }

            void MakeStackedTrafokreise()
            {
                var trafoKreise = archiveEntries.Where(x => x.Key.SumType == SumType.ByTrafokreis).ToList();
                List<LineSeriesEntry> lines = new List<LineSeriesEntry>();
                var runningProfile = Profile.MakeConstantProfile(0, "base", Profile.ProfileResolution.QuarterHour);
                foreach (var entry in trafoKreise) {
                    runningProfile = runningProfile.Add(entry.Profile.ConvertFromEnergyToPower(), entry.Name);
                    lines.Add(runningProfile.GetLineSeriesEntry());
                }

                Profile bkw = new Profile(bkwRaw[0].Profile);
                lines.Add(bkw.GetLineSeriesEntry());
                var fn = MakeAndRegisterFullFilename("TrafokreiseStacked.png", slice);
                Services.PlotMaker.MakeLineChart(fn, "energy", lines, new List<AnnotationEntry>());
            }
        }
    }
}