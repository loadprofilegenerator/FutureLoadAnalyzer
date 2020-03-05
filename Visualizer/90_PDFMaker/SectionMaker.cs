using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Common.ResultFiles;
using JetBrains.Annotations;
using MigraDoc.DocumentObjectModel;

namespace BurgdorfStatistics._90_PDFMaker {
    internal class SectionMaker {
        public void MakePage([NotNull] Document doc, [ItemNotNull] [NotNull] List<ResultFileEntry> resultFileEntries)
        {
            var sectionDescriptions = resultFileEntries.Select(x => x.SectionDescription).Distinct().ToList();
            if (sectionDescriptions.Count != 1) {
                throw new Exception("Multiple different section descriptions for section " + resultFileEntries[0].Scenario + " " + resultFileEntries[0].Section);
            }

            var sec = MakeDescriptionArea(doc, resultFileEntries[0].Section, resultFileEntries[0].SectionDescription);
            foreach (var fileEntry in resultFileEntries) {
                if (fileEntry.FileTitle != null) {
                    var para = sec.AddParagraph();
                    para.Format.Alignment = ParagraphAlignment.Justify;
                    para.Format.Font.Name = "Times New Roman";
                    para.Format.Font.Size = 10;
                    para.Format.Font.Bold = true;
                    para.Format.SpaceAfter = "0.25cm";
                    para.Format.Font.Color = Colors.Black;

                    para.AddText(fileEntry.FileTitle);
                }

                if (fileEntry.FileDescription != null) {
                    var para = sec.AddParagraph();
                    para.Format.Alignment = ParagraphAlignment.Left;
                    para.Format.Font.Name = "Times New Roman";
                    para.Format.Font.Size = 10;
                    para.Format.Font.Bold = false;
                    para.Format.SpaceAfter = "0.25cm";
                    para.Format.Font.Color = Colors.Black;

                    para.AddText(fileEntry.FileDescription);
                }

                AddImageToSection(sec, fileEntry);
            }
        }


        protected static void AddImageToSection([NotNull] Section sec, [NotNull] ResultFileEntry rfe)
        {
            var mytitle = rfe.FileTitle;
            var imgtitle = sec.AddParagraph(mytitle);
            imgtitle.Format.Font.Size = 12;
            imgtitle.Format.KeepWithNext = true;
            imgtitle.Format.SpaceAfter = "0.5cm";
            imgtitle.Format.SpaceBefore = "0.5cm";
            imgtitle.Format.Font.Color = Colors.Blue;
            var img = sec.AddImage(rfe.FullFilename);
            var size = GetDimensions(rfe.FullFilename);
            if (size.Height > 5000) {
                img.Height = "20cm";
            }
            else if (size.Height > size.Width) {
                img.Height = "10cm";
            }
            else {
                img.Width = "16cm";
            }
        }

        [NotNull]
        protected static Size GetDimensions([NotNull] string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                var width = bitmapFrame.PixelWidth;
                var height = bitmapFrame.PixelHeight;
                return new Size(height, width);
            }
        }

        [NotNull]
        protected Section MakeDescriptionArea([NotNull] Document doc, [NotNull] string sectionTitle, [CanBeNull] string sectionDescription)
        {
            var sec = doc.AddSection();
            // Add a single paragraph with some text and format information.
            var para = MakeParagraph(sec, 20, SpaceAfter.Large);
            para.AddText(sectionTitle);
            para.AddBookmark(sectionTitle);
            if (sectionDescription != null) {
                para = MakeParagraph(sec, 12);
                para.AddText(sectionDescription);
            }

            return sec;
        }

        private enum SpaceAfter {
            Large,
            Small
        }

        [NotNull]
        private static Paragraph MakeParagraph([NotNull] Section sec, int fontSize, SpaceAfter spaceAfter = SpaceAfter.Small)
        {
            var para = sec.AddParagraph();
            para.Format.Alignment = ParagraphAlignment.Justify;
            para.Format.Font.Name = "Arial";
            para.Format.Font.Size = fontSize;
            para.Format.Font.Bold = false;
            switch (spaceAfter) {
                case SpaceAfter.Large:
                    para.Format.SpaceAfter = "0.5cm";
                    break;
                case SpaceAfter.Small:
                    para.Format.SpaceAfter = "0.25cm";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spaceAfter), spaceAfter, null);
            }

            para.Format.Font.Color = Colors.Black;
            return para;
        }

        protected class Size {
            public Size(double height, double width)
            {
                Height = height;
                Width = width;
            }

            public double Height { get; }
            public double Width { get; }
        }
    }
}