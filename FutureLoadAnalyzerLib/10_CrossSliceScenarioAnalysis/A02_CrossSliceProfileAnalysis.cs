using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._08_ProfileGeneration;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._10_CrossSliceScenarioAnalysis {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class A02_CrossSliceProfileAnalysis : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A02_CrossSliceProfileAnalysis([NotNull] ServiceRepository services)
            : base(nameof(A02_CrossSliceProfileAnalysis), Stage.CrossSliceProfileAnalysis, 102, services,true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices, [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make trafostation results");
            MultiyearTrend myt = new MultiyearTrend();
            foreach (var slice in allSlices) {
                Info("Processing slice " + slice);
                var dbArchiving = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileAnalysis, slice, DatabaseCode.Smartgrid);
                var saArchiveEntry = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchiving, SaveableEntryTableType.Smartgrid, Services.Logger);
                var sgis = dbArchiving.Fetch<SmartGridInformation>();
                if (sgis.Count != 1) {
                    throw new FlaException("invalid count");
                }
                var sgi = sgis[0];
                myt[slice].AddValue("Gesamtspeichergrösse [GWh]",sgi.TotalStorageSize, DisplayUnit.GWh);
                double avgreduction = sgi.SummedReductionFactor / sgi.NumberOfReductionFactors;
                myt[slice].AddValue("Average Reduction Factor", avgreduction, DisplayUnit.Stk);
                myt[slice].AddValue("Number of Prosumers", sgi.NumberOfProsumers, DisplayUnit.Stk);
                var aes =  saArchiveEntry.LoadAllOrMatching();
                {
                    var cityload = aes.Single(x => x.Name == SummedLoadType.CityLoad.ToString());
                    myt[slice].AddValue("Energiebedarf Gesamt [GWh]", cityload.Profile.EnergySum(), DisplayUnit.GWh);
                    var cityGen1 = aes.Single(x => x.Name == SummedLoadType.CityGeneration.ToString());
                    var cityGenProf = cityGen1.Profile.MultiplyWith(-1, "Energieerzeugung");
                    if (cityGenProf.EnergySum() > 0) {
                        throw new FlaException("Positive energy sum while generationg");
                    }
                    myt[slice].AddValue("Energieerzeugung Gesamt [GWh]", cityGenProf.EnergySum(), DisplayUnit.GWh);
                    var citySum = cityload.Profile.Add(cityGenProf, "sum");

                    myt[slice].AddValue("Netto-Energiebedarf [GWh]", citySum.EnergySum(), DisplayUnit.GWh);
                    myt[slice].AddValue("Maximale Last am Umspannwerk [MW]", citySum.MaxPower(), DisplayUnit.Mw);
                    myt[slice].AddValue("Minimale Last am Umspannwerk [MW]", citySum.MinPower(), DisplayUnit.Mw);

                }

                var smartcityload = aes.Single(x => x.Name == SummedLoadType.SmartCityLoad.ToString());
                myt[slice].AddValue("Energiebedarf Gesamt (smart) [GWh]", smartcityload.Profile.EnergySum(), DisplayUnit.GWh);
                var smartcityGen = aes.Single(x => x.Name == SummedLoadType.SmartCityGeneration.ToString());
                myt[slice].AddValue("Energieerzeugung Gesamt (smart) [GWh[", smartcityGen.Profile.EnergySum(), DisplayUnit.GWh);
                var smartcitySum = smartcityload.Profile.Add(smartcityGen.Profile, "sum");

                myt[slice].AddValue("Netto-Energiebedarf (smart) [GWh]", smartcitySum.EnergySum(), DisplayUnit.GWh);
                myt[slice].AddValue("Maximale Last am Umspannwerk (smart) [MW]", smartcitySum.MaxPower(), DisplayUnit.Mw);
                myt[slice].AddValue("Minimale Last am Umspannwerk (smart) [MW]", smartcitySum.MinPower(), DisplayUnit.Mw);
            }
            var filename3 = MakeAndRegisterFullFilename("SmartGridOverview.xlsx",Constants.PresentSlice);
            Info("Writing results to " + filename3);
            XlsxDumper.DumpMultiyearTrendToExcel(filename3, myt);
            SaveToPublicationDirectory(filename3,Constants.PresentSlice,"4.5");
        }
    }
}