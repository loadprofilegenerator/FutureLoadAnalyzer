using System.Reflection;
using Common.Steps;
using JetBrains.Annotations;
using MigraDoc.DocumentObjectModel;

namespace BurgdorfStatistics._90_PDFMaker {
    internal class CoverPage {
        [CanBeNull] private readonly string _version = Assembly.GetAssembly(typeof(CoverPage)).GetName().Version.ToString();

        public void MakePage([NotNull] Document doc, Scenario scenario)
        {
            var section = doc.AddSection();

            section.AddParagraph();
            var paragraph = section.AddParagraph("Gesamtüberblick Scenario " + scenario.ToString());

            paragraph.Format.Font.Size = 16;
            paragraph.Format.Font.Color = Colors.DarkRed;
            paragraph.Format.SpaceBefore = "8cm";
            paragraph.Format.SpaceAfter = "3cm";

            paragraph = section.AddParagraph("FutureLoadAnalyzer " + _version);
            paragraph.Format.Font.Size = 12;
            paragraph.Format.Font.Color = Colors.Black;
            paragraph.Format.SpaceBefore = "1cm";
            paragraph.Format.SpaceAfter = "1cm";
            paragraph = section.AddParagraph("by Noah Pflugradt");
            paragraph.Format.Font.Size = 10;
            paragraph.Format.Font.Color = Colors.Black;
            paragraph.Format.SpaceBefore = "1cm";
            paragraph.Format.SpaceAfter = "1cm";

            paragraph = section.AddParagraph("http://www.loadprofilegenerator.ch");
            paragraph.Format.Font.Size = 12;
            paragraph.Format.Font.Color = Colors.Blue;
            paragraph.Format.SpaceBefore = "1cm";
            paragraph.Format.SpaceAfter = "1cm";

            paragraph = section.AddParagraph("Rendering date:");
            paragraph.AddDateField();
        }
    }
}