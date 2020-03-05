using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.Plotly;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using MessagePack;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    public enum SummedLoadType {
        CityLoad,
        CityGeneration,
        SmartCityLoad,
        SmartCityGeneration
    }

    // ReSharper disable once InconsistentNaming
    public class E_ApplySmartGridToGeneratedProfiles : RunableForSingleSliceWithBenchmark {
        public const int MySequenceNumber = 500;

        public E_ApplySmartGridToGeneratedProfiles([JetBrains.Annotations.NotNull] ServiceRepository services) : base(
            nameof(E_ApplySmartGridToGeneratedProfiles),
            Stage.ProfileGeneration,
            MySequenceNumber,
            services,
            false,
            null)
        {
        }

        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            var dbProfileExport = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.HouseProfiles);
            var saLoad = SaveableEntry<Prosumer>.GetSaveableEntry(dbProfileExport, SaveableEntryTableType.HouseLoad, Services.Logger);
            var saGeneration = SaveableEntry<Prosumer>.GetSaveableEntry(dbProfileExport, SaveableEntryTableType.HouseGeneration, Services.Logger);
            var dbArchiving = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileAnalysis, slice, DatabaseCode.Smartgrid);
            dbArchiving.RecreateTable<SmartGridInformation>();
            var saArchiveEntry = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchiving, SaveableEntryTableType.Smartgrid, Services.Logger);
            int count = 0;
            var smartSlice = slice.CopyThisSlice();
            smartSlice.SmartGridEnabled = true;
            RowCollection prosumerCollection = new RowCollection("Prosumers", "Prosumers");
            Stopwatch sw = Stopwatch.StartNew();
            const string tkfield = "Trafokreis";
            Stopwatch swIdx = Stopwatch.StartNew();
            saLoad.CreateIndexIfNotExists(tkfield);
            saGeneration.CreateIndexIfNotExists(tkfield);
            Info("Creating the index took " + swIdx.Elapsed);
            var trafos = saLoad.SelectSingleDistinctField<string>(tkfield);
            Info("Reading trafokreise took " + sw.Elapsed);
            Dictionary<string, Profile> trafokreiseLoad = new Dictionary<string, Profile>();
            Dictionary<string, Profile> trafokreiseGeneration = new Dictionary<string, Profile>();
            ChangableProfile cityload = ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));
            ChangableProfile cityGeneration =
                ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));
            ChangableProfile smartCityLoad =
                ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));
            ChangableProfile smartCityGeneration =
                ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));
            RowCollection smartGridPointValues = new RowCollection("SmartGridHASummaries", "SmartGridHASummaries");
            SmartGridInformation smartGridInformation = new SmartGridInformation();
            foreach (var trafo in trafos) {
                //if (count > 500) {
                    //continue;
                //}
                Dictionary<string, List<Prosumer>> prosumersByHa = new Dictionary<string, List<Prosumer>>();
                ChangableProfile trafoLoadSum =
                    ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));
                foreach (var prosumer in saLoad.ReadSubsetOfTableDBAsEnumerable(tkfield, trafo)) {
                    RowBuilder rb = RowBuilder.Start("Name", prosumer.Name);
                    rb.Add("Energy", prosumer.Profile?.EnergySum());
                    rb.Add("HA", prosumer.HausanschlussKey);
                    rb.Add("Trafokreis", prosumer.TrafoKreis);
                    prosumerCollection.Add(rb);
                    count++;
                    if (count % 50 == 0) {
                        Info("Processing Prosumers Load: " + count + " " + sw.Elapsed);
                    }

                    if (!string.IsNullOrWhiteSpace(prosumer.HausanschlussKey)) {
                        if (!prosumersByHa.ContainsKey(prosumer.HausanschlussKey)) {
                            prosumersByHa.Add(prosumer.HausanschlussKey, new List<Prosumer>());
                        }

                        prosumersByHa[prosumer.HausanschlussKey].Add(prosumer);
                    }

                    trafoLoadSum.Add(prosumer.Profile ?? throw new FlaException("empty profile"));
                }

                AnalysisKey keyload = new AnalysisKey(trafo, null, SumType.ByTrafokreis, GenerationOrLoad.Load, null, null, null);
                ArchiveEntry aeload = new ArchiveEntry("Trafokreis " + trafo, keyload, trafoLoadSum.ToProfile(), GenerationOrLoad.Load, trafo);
                saArchiveEntry.AddRow(aeload);

                trafokreiseLoad.Add(trafo, trafoLoadSum.ToProfile());
                var trafoGenerationSum = Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour);
                foreach (var prosumer in saGeneration.ReadSubsetOfTableDBAsEnumerable(tkfield, trafo)) {
                    RowBuilder rb = RowBuilder.Start("Name", prosumer.Name);
                    rb.Add("Energy", prosumer.Profile?.EnergySum());
                    rb.Add("HA", prosumer.HausanschlussKey);
                    rb.Add("Trafokreis", prosumer.TrafoKreis);
                    prosumerCollection.Add(rb);
                    if (count % 50 == 0) {
                        Info("Processing Prosumers Generation: " + count + " " + sw.Elapsed);
                    }

                    if (!string.IsNullOrWhiteSpace(prosumer.HausanschlussKey)) {
                        if (!prosumersByHa.ContainsKey(prosumer.HausanschlussKey)) {
                            prosumersByHa.Add(prosumer.HausanschlussKey, new List<Prosumer>());
                        }

                        prosumersByHa[prosumer.HausanschlussKey].Add(prosumer);
                    }

                    //var powerLimited = prosumer.Profile?.LimitToPercentageOfMax(0.5)?? throw new FlaException("blub");
                    //if(powerLimited.Values.Count ==0) { throw new FlaException("huch?");}
                    trafoGenerationSum = trafoGenerationSum.Add(prosumer.Profile ?? throw new FlaException(), "Sum");
                }

                AnalysisKey key = new AnalysisKey(trafo, null, SumType.ByTrafokreis, GenerationOrLoad.Generation, null, null, null);
                ArchiveEntry ae = new ArchiveEntry("Trafokreis " + trafo, key, trafoGenerationSum, GenerationOrLoad.Generation, trafo);
                saArchiveEntry.AddRow(ae);
                trafokreiseGeneration.Add(trafo, trafoGenerationSum);
                cityload.Add(trafoLoadSum);
                cityGeneration.Add(trafoGenerationSum);
                //if (count > 16000) {
                ApplySmartGridstuff(prosumersByHa, trafo, smartCityLoad, smartCityGeneration, smartGridPointValues, smartGridInformation, smartSlice);
                //}
            }

            var addedSmart = smartCityLoad.ToProfile().Add(smartCityGeneration.ToProfile(), "Netto-Last (smart)");
            var addedSerializeFn = MakeAndRegisterFullFilename("addedProfile.lz4", slice);
            dbArchiving.BeginTransaction();
            dbArchiving.Save(smartGridInformation);
            dbArchiving.CompleteTransaction();
            SaveCityProfile(cityload, saArchiveEntry, SummedLoadType.CityLoad, GenerationOrLoad.Load);
            SaveCityProfile(cityGeneration, saArchiveEntry, SummedLoadType.CityGeneration, GenerationOrLoad.Generation);
            SaveCityProfile(smartCityGeneration, saArchiveEntry, SummedLoadType.SmartCityGeneration, GenerationOrLoad.Generation);
            SaveCityProfile(smartCityLoad, saArchiveEntry, SummedLoadType.SmartCityLoad, GenerationOrLoad.Load);
            saArchiveEntry.MakeCleanTableForListOfFields(false);
            saArchiveEntry.SaveDictionaryToDatabase(MyLogger);

            FileStream fs = new FileStream(addedSerializeFn, FileMode.Create);
            var added = cityload.ToProfile().Subtract(cityGeneration.ToProfile(), "Netto-Last (konventionell)");
            var lz4Arr = LZ4MessagePackSerializer.Serialize(added);
            fs.Write(lz4Arr, 0, lz4Arr.Length);
            fs.Close();
            MakePlotlyTrafostationBoxPlots("Boxplot_Load_OhneMV.html", trafokreiseLoad, slice, true);
            MakePlotlyTrafostationBoxPlots("Boxplot_Gen_OhneMV.html", trafokreiseGeneration, slice, true);

            var fn = MakeAndRegisterFullFilename("ProsumerDump.xlsx", slice);
            XlsxDumper.DumpProfilesToExcel(fn,
                slice.DstYear,
                15,
                new RowWorksheetContent(prosumerCollection),
                new RowWorksheetContent(smartGridPointValues));
            var fnProf = MakeAndRegisterFullFilename("SmartGridProfiles_distributedStorage.xlsx", slice);
            RowCollection rc = new RowCollection("StatusInfo", "Status");
            double avgReductionFactor = smartGridInformation.SummedReductionFactor / smartGridInformation.NumberOfReductionFactors;
            rc.Add(RowBuilder.Start("Total storage size", smartGridInformation.TotalStorageSize)
                .Add("Number of Prosumers", smartGridInformation.NumberOfProsumers)
                .Add("Citywide Reduction", avgReductionFactor)
                .Add("MinimumLoadBefore",added.MinPower())
                .Add("MinLoadSmart", addedSmart.MinPower())
                .Add("MaxLoadBefore", added.MaxPower())
                .Add("MaxLoadSmart", addedSmart.MaxPower())
            );

            XlsxDumper.DumpProfilesToExcel(fnProf,
                slice.DstYear,
                15,
                new RowWorksheetContent(rc),
                new ProfileWorksheetContent("load", "Last [MW]", 240, cityload.ToProfile()),
                new ProfileWorksheetContent("generation", "Erzeugung [MW]", 240, cityGeneration.ToProfile()),
                new ProfileWorksheetContent("added", "Netto-Last [kW]", 240, added),
                new ProfileWorksheetContent("smartload", "Last (smart) [MW]", 240, smartCityLoad.ToProfile()),
                new ProfileWorksheetContent("smartgeneration", "Erzeugung (smart) [MW]", 240, smartCityGeneration.ToProfile()),
                new ProfileWorksheetContent("smartadded", "Netto-Last [kW]", 240, added, addedSmart));
            SaveToArchiveDirectory(fnProf, RelativeDirectory.Report, smartSlice);
            SaveToPublicationDirectory(fnProf, slice, "4.5");
            SaveToPublicationDirectory(fnProf, slice, "5");
        }

        private void ApplySmartGridstuff([JetBrains.Annotations.NotNull] Dictionary<string, List<Prosumer>> dictionary,
                                         [JetBrains.Annotations.NotNull] string trafokreis,
                                         [JetBrains.Annotations.NotNull] ChangableProfile smartLoad,
                                         [JetBrains.Annotations.NotNull] ChangableProfile smartGeneration,
                                         [JetBrains.Annotations.NotNull] RowCollection smartGridPointValues,
                                         [JetBrains.Annotations.NotNull] SmartGridInformation summedSgi,
                                         [JetBrains.Annotations.NotNull] ScenarioSliceParameters smartSlice)
        {
            trafokreis = FilenameHelpers.CleanUmlaute(trafokreis);
            var loadFilename = MakeAndRegisterFullFilename(trafokreis + ".load.csv", smartSlice, false);
            var generationFilename = MakeAndRegisterFullFilename(trafokreis + ".generation.csv", smartSlice, false);
            using (StreamWriter loadsw = new StreamWriter(loadFilename)) {
                using (StreamWriter generationsw = new StreamWriter(generationFilename)) {
                    foreach (var ha in dictionary) {
                        var loadsum = ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));
                        var gensum = ChangableProfile.FromProfile(Profile.MakeConstantProfile(0, "Sum", Profile.ProfileResolution.QuarterHour));

                        foreach (var prosumer in ha.Value) {
                            if (prosumer.GenerationOrLoad == GenerationOrLoad.Load) {
                                loadsum.Add(prosumer.Profile ?? throw new FlaException());
                            }
                            else {
                                gensum.Subtract(prosumer.Profile ?? throw new FlaException());
                            }
                        }

                        RowBuilder rb = RowBuilder.Start("Hausanschluss", ha.Key);
                        smartGridPointValues.Add(rb);
                        var loadProfile = loadsum.ToProfile();
                        rb.Add("Load Energy Sum [kWh]", loadProfile.EnergySum());
                        var genProfile = gensum.ToProfile();
                        rb.Add("Generation Energy Sum [kWh]", genProfile.EnergySum());
                        rb.Add("Number of Prosumers", ha.Value.Count);

                        var sum = loadProfile.Add(genProfile, "sum");
                        double storagesizeLoad = //Math.Abs(sum.MakeDailyAverages().Values.Max()) * 24 * 4 * 1.5;
                            loadProfile.EnergySum() / 1000 * 4;
                        double storageSizeGen = Math.Abs(genProfile.EnergySum()) / 1000 * 4;
                        double storagesize = Math.Max(storageSizeGen, storagesizeLoad);
                        sum = ProfileSmoothing.FindBestPowerReductionRatio(sum, storagesize, out _, out var reductionFactor, 0.7);
                        //if (reductionFactor > 0.99) {
//                            storagesize = 0;
                        //}
                        summedSgi.NumberOfReductionFactors++;
                        summedSgi.SummedReductionFactor += reductionFactor;
                        summedSgi.TotalStorageSize += storagesize;
                        summedSgi.NumberOfProsumers++;
                        rb.Add("Trafokreis", trafokreis);
                        rb.Add("Faktor", reductionFactor);
                        rb.Add("Storage Size", storagesize);
                        var positive = sum.GetOnlyPositive("positive");
                        smartLoad.Add(positive);
                        var negative = sum.GetOnlyNegative("negative");
                        smartGeneration.Add(negative);
                        rb.Add("Energy Sum  Size", storagesize);
                        string csvhakey = ha.Value.First().Isn + ";SM-Pros;MAXMEASUREDVALUE;" + ha.Key + ";";
                        if (positive.EnergySum() > 0) {
                            loadsw.WriteLine(csvhakey + positive.GetCSVLine());
                        }

                        if (negative.EnergySum() < 0) {
                            var generation = negative.MultiplyWith(-1, "generation");
                            generationsw.WriteLine(csvhakey + generation.GetCSVLine());
                        }

                        //positive.GetCSVLine()
                    }

                    loadsw.Close();
                    generationsw.Close();
                }
            }

            if (smartSlice.DstYear == 2050) {
                FileInfo lfn = new FileInfo(loadFilename);
                if (lfn.Length > 0) {
                    SaveToArchiveDirectory(loadFilename, RelativeDirectory.Load, smartSlice);
                }

                var gfn = new FileInfo(generationFilename);
                if (gfn.Length > 0) {
                    SaveToArchiveDirectory(generationFilename, RelativeDirectory.Generation, smartSlice);
                }
            }
        }

        private void MakePlotlyTrafostationBoxPlots([JetBrains.Annotations.NotNull] string fn,
                                                    [JetBrains.Annotations.NotNull] Dictionary<string, Profile> trafokreisProfile,
                                                    [JetBrains.Annotations.NotNull] ScenarioSliceParameters slice,
                                                    bool skipMv)
        {
            if (trafokreisProfile.Count == 0) {
                throw new FlaException("empty profiles");
            }

            var fn2 = MakeAndRegisterFullFilename(fn, slice);
            List<BoxplotTrace> bpts = new List<BoxplotTrace>();
            int height = 100;
            foreach (var entry in trafokreisProfile) {
                if (skipMv && entry.Key == "MV_customers") {
                    continue;
                }

                BoxplotTrace bpt = new BoxplotTrace(entry.Key, entry.Value.ConvertFromEnergyToPower().Get5BoxPlotValues());
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

        private static void SaveCityProfile([JetBrains.Annotations.NotNull] ChangableProfile cityload,
                                            [JetBrains.Annotations.NotNull] SaveableEntry<ArchiveEntry> saArchiveEntry,
                                            SummedLoadType name,
                                            GenerationOrLoad generationOrLoad)
        {
            AnalysisKey key1 = new AnalysisKey(null, null, SumType.ByCity, generationOrLoad, null, name.ToString(), null);
            ArchiveEntry ae1 = new ArchiveEntry(name.ToString(), key1, cityload.ToProfile(), generationOrLoad, "City");
            saArchiveEntry.AddRow(ae1);
        }
    }
}