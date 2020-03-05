using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.ResultFiles;
using Common.Steps;
using JetBrains.Annotations;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace BurgdorfStatistics._90_PDFMaker {
    public class MigraPDFCreator : RunnableForScenarioWithBenchmark {
        private const bool Startpdf = true;


        [NotNull]
        private Document CreateDocument()
        {
            // Create a new MigraDoc document
            var document = new Document {
                Info = {
                    Title = "FutureLoadAnalyzer",
                    Subject = "Overview",
                    Author = "Noah Pflugradt"
                }
            };
            return document;
        }

        [SuppressMessage("ReSharper", "ReplaceWithSingleAssignment.True")]
        private static void ProcessScenario([NotNull] Document document, Scenario scenario)
        {
            CheckIfAllFilesAreRegistered();

            var scenarioRfes = ResultFileEntry.LoadAllForScenario(scenario);
            var cp = new CoverPage();
            cp.MakePage(document, scenario);
            var sections = scenarioRfes.Select(x => x.Section).Distinct().ToList();
            foreach (var section in sections) {
                var scm = new SectionMaker();
                var sectionRfes = scenarioRfes.Where(x => x.Section == section).ToList();
                scm.MakePage(document, sectionRfes);
            }
        }

        public static void CheckIfAllFilesAreRegistered()
        {
            var di = new DirectoryInfo(Constants.BasePath);
            var pngs = di.GetFiles("*.png", SearchOption.AllDirectories);
            var rfes = ResultFileEntry.LoadAllForScenario(null);
            foreach (var info in pngs) {
                if (!rfes.Any(x => x.FullFilename == info.FullName)) {
                    throw new Exception("Forgotten to register " + info.FullName);
                }
            }
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> scenarioSliceList)
        {
            ScenarioSliceParameters slice = scenarioSliceList.Last();
            // Create a MigraDoc document
            var pdfDstPath = FilenameHelpers.GetTargetDirectory(Stage.PDF, SequenceNumber, Name, slice);
            var document = CreateDocument();
            ProcessScenario(document, slice.DstScenario);
            var renderer = new PdfDocumentRenderer(true) {
                Document = document
            };
            renderer.RenderDocument();
            // Save the document...
            if (!Directory.Exists(pdfDstPath)) {
                Directory.CreateDirectory(pdfDstPath);
            }

            var filename = "Report." +slice.DstScenario + ".pdf";
            var dstFullName = Path.Combine(pdfDstPath, filename);

            if (File.Exists(dstFullName)) {
                File.Delete(dstFullName);
            }

            renderer.PdfDocument.Save(dstFullName);
            GC.WaitForPendingFinalizers();
            GC.Collect();
            // ...and start a viewer.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Startpdf) {
                Process.Start(dstFullName);
            }
        }

        public MigraPDFCreator([NotNull] ServiceRepository services)
            : base("MigraDocPDFCreator", Stage.PDF, 1, services, false)
        {
        }
    }
}