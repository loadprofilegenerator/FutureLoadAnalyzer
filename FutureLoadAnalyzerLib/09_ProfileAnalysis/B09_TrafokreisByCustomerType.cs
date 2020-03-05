using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B09_TrafokreisByCustomerType : RunableForSingleSliceWithBenchmark {
        public B09_TrafokreisByCustomerType([NotNull] ServiceRepository services) : base(nameof(B09_TrafokreisByCustomerType),
            Stage.ProfileAnalysis,
            209,
            services,
            false)
        {
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbArchive = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ProfileGeneration, slice, DatabaseCode.SummedLoadForAnalysis);
            var saHouses = SaveableEntry<ArchiveEntry>.GetSaveableEntry(dbArchive, SaveableEntryTableType.SummedLoadsForAnalysis, Services.Logger);
            var entries = saHouses.LoadAllOrMatching();
            var providerentries = entries.Where(x => x.Key.SumType == SumType.ByTrafokreisAndProvider).ToList();
            RowCollection rc = new RowCollection("energy","energy");
            Dictionary<string, RowBuilder> rbsByTk = new Dictionary<string, RowBuilder>();
            foreach (var providerentry in providerentries) {
                var tk = providerentry.Key.Trafokreis;
                RowBuilder rb;
                if (!rbsByTk.ContainsKey(tk ?? throw new InvalidOperationException())) {
                    rb = RowBuilder.Start("Trafokreis", providerentry.Key.Trafokreis);
                    rbsByTk.Add(tk,rb);
                }
                else {
                    rb = rbsByTk[tk];
                }
                var provider = ChartHelpers.GetFriendlyProviderName(providerentry.Key.ProviderType + " " + providerentry.GenerationOrLoad);
                var factor = 1;
                if (providerentry.GenerationOrLoad == GenerationOrLoad.Generation) {
                    factor = -1;
                }

                rb.AddToPossiblyExisting(provider, providerentry.Profile.EnergySum()*factor);
            }

            foreach (var pair in rbsByTk) {
                rc.Add(pair.Value);
            }
            var fn = MakeAndRegisterFullFilename("EnergyPerTrafokreis.xlsx", slice);
            XlsxDumper.WriteToXlsx(fn, rc);
            SaveToPublicationDirectory(fn, slice, "4.2");
            Info("saved " + fn);
        }


    }
}