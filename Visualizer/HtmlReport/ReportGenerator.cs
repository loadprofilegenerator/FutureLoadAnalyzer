using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using Common;
using Common.Config;
using Common.Logging;
using Common.ResultFiles;
using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer.HtmlReport {
    public class ReportGenerator : BasicLoggable {
        private const string Resources = "resources";

        public ReportGenerator([NotNull] Logger logger) : base(logger, Stage.Reporting, "ReportGenerator")
        {
        }


        public void Run([NotNull] RunningConfig rc)
        {
            Info("loading all result files");
            var rfes = FlaResultFileEntry.LoadAllForScenario(null, rc);
            rfes = rfes.Where(x => x.FullFilename.ToLower(CultureInfo.InvariantCulture).EndsWith(".png")).ToList();
            Dictionary<FlaResultFileEntry, string> dstFileNameByRfe = new Dictionary<FlaResultFileEntry, string>();
            Dictionary<FlaResultFileEntry, string> dstFileNameSmallByRfe = new Dictionary<FlaResultFileEntry, string>();
            Info("making small pictures");
            foreach (var rf in rfes) {
                string dstfn = rf.GetUniqueFilename();
                dstFileNameByRfe.Add(rf, dstfn);
                string dstNameSmall = dstfn.Replace(".png", ".small.png");
                dstFileNameSmallByRfe.Add(rf, dstNameSmall);
            }

            //todo:check if all files are referenced and throw exception for missing files
            var fns = dstFileNameByRfe.Values.Distinct().ToList();
            if (fns.Count != dstFileNameByRfe.Count) {
                throw new FlaException("No unique names");
            }

            var scenarios = rfes.Select(x => x.Scenario).Distinct().ToList();
            foreach (var scenario in scenarios) {
                Info("writing html for scenario " + scenario);
                var f1Rfes = rfes.Where(x => x.Scenario == scenario).ToList();
                string dstDirectory = Path.Combine(rc.Directories.BaseProcessingDirectory, "Reports", scenario.ToString());
                string dstDirectoryResources = Path.Combine(dstDirectory, Resources);
                if (Directory.Exists(dstDirectory)) {
                    try {
                        DirectoryInfo mydi = new DirectoryInfo(dstDirectory);
                        var files = mydi.GetFiles("*.*");
                        foreach (var file in files) {
                            file.Delete();
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Error("ex:" + ex.Message);
                    }

                    Thread.Sleep(250);
                }

                if (!Directory.Exists(dstDirectoryResources)) {
                    Directory.CreateDirectory(dstDirectoryResources);
                }

                Thread.Sleep(500);
                string bootstrapDirCss = Path.Combine(rc.Directories.BaseUserSettingsDirectory, @"bootstrap-4.0.0\dist\css");
                CollectFiles(dstDirectoryResources, bootstrapDirCss, "bootstrap.min.css");
                string bootstrapDirjs = Path.Combine(rc.Directories.BaseUserSettingsDirectory, @"bootstrap-4.0.0\dist\js");
                CollectFiles(dstDirectoryResources, bootstrapDirjs, "jquery-3.2.1.slim.min.js");
                CollectFiles(dstDirectoryResources, bootstrapDirjs, "popper.min.js");
                CollectFiles(dstDirectoryResources, bootstrapDirjs, "bootstrap.min.js");
                List<Task> tasks = new List<Task>();
                foreach (var entry in f1Rfes) {
                    string dstfn = dstFileNameByRfe[entry];
                    string dstFullname = Path.Combine(dstDirectoryResources, dstfn);
                    if (!File.Exists(dstFullname)) {
                        string dstfullnameSmall = Path.Combine(dstDirectoryResources, dstFileNameSmallByRfe[entry]);
                        Info("copying " + dstFullname);
                        File.Copy(entry.FullFilename, dstFullname);
                        var t = Task.Run(() => ResizeImage(dstFullname, 400, dstfullnameSmall));
                        tasks.Add(t);
                    }
                }

                while (tasks.Count > 0) {
                    var t = tasks[0];
                    tasks.RemoveAt(0);
                    t.Wait();
                }

                Info("Making scenario report");
                MakeScenarioReport(scenario, f1Rfes, dstDirectory, dstFileNameByRfe, dstFileNameSmallByRfe);
                Info("finished scenario report");
            }
        }

        [NotNull]
        private static string CleanStageName([NotNull] string stage) =>
            stage.Replace("_", "").Replace(" ", "");

        private void CollectFiles([NotNull] string dstDirectory, [NotNull] string srcDir, [NotNull] string srcFile)
        {
            string srcFullFile = Path.Combine(srcDir, srcFile);
            if (!File.Exists(srcFullFile)) {
                throw new FlaException("file not found");
            }

            var srcFileI = new FileInfo(srcFullFile);
            string dstfileName = Path.Combine(dstDirectory, srcFile);
            if (File.Exists(dstfileName)) {
                return;
            }

            Info("copying to " + dstfileName);
            srcFileI.CopyTo(dstfileName);
        }

        private static void MakeScenarioReport([NotNull] Scenario scenario,
                                               [NotNull] [ItemNotNull] List<FlaResultFileEntry> f1Rfes,
                                               [NotNull] string dstDirectory,
                                               [NotNull] Dictionary<FlaResultFileEntry, string> dstFileNameByRfe,
                                               [NotNull] Dictionary<FlaResultFileEntry, string> dstFileNameSmallByRfe)
        {
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb)) {
                using (var w = new HtmlTextWriter(sw)) {
                    w.WriteLine("<!doctype html>");
                    w.Html(new {lang = "en"}).Head().Meta(new {charset = "utf-8"}).EndTag().Meta(new {
                        name = "viewport",
                        content = "width=device-width, initial-scale=1, shrink-to-fit=no"
                    }).EndTag().Link(new {rel = "stylesheet", href = Resources + "/bootstrap.min.css"}).EndTag().EndTag().Body().WriteLine();
                    MakeTopNavBar(scenario, w);
                    w.Div(new {@class = "container-fluid"});
                    w.Div(new {@class = "row"});
                    var years = f1Rfes.Select(x => x.Year).Distinct().ToList();
                    WriteSideNavBar(f1Rfes, w, years);
                    w.Main(new {role = "main", @class = "col-md-9 ml-sm-auto col-lg-10 pt-3 px-4"});
                    w.H1().WriteContent("Report für Scenario " + scenario).EndTag();
                    foreach (var year in years) {
                        var f2Fres = f1Rfes.Where(x => x.Year == year).ToList();
                        WriteYearSection(w, year, f2Fres, dstFileNameByRfe, dstFileNameSmallByRfe);
                    }

                    w.EndTag(); //main
                    w.EndTag(); //main row
                    w.EndTag(); //container div

                    w.Script(new {src = Resources + "/jquery-3.2.1.slim.min.js"}).EndTag();
                    w.Script(new {src = Resources + "/popper.min.js"}).EndTag();
                    w.Script(new {src = Resources + "/bootstrap.min.js"}).EndTag();

                    w.EndTag().EndTag();
                }
            }

            string dstFn = Path.Combine(dstDirectory, scenario + ".html");
            File.WriteAllText(dstFn, sb.ToString());
        }

        private static void MakeTopNavBar([NotNull] Scenario scenario, [NotNull] HtmlTextWriter w)
        {
            w.Nav(new {@class = "navbar navbar-dark sticky-top bg-dark flex-md-nowrap p-0"});
            w.A(new {@class = "navbar-brand col-sm-3 col-md-2 mr-0", href = "#"}).WriteContent("SimZukunft").EndTag();
            w.A(new {@class = "w-100 navbar-brand", href = "#top"}).WriteContent("Report für Szenario " + scenario).EndTag();
            w.Ul(new {@class = "navbar-nav px-3"});
            w.Li(new {@class = "nav-item text-nowrap"});
            w.A(new {@class = "nav-link", href = "#"}).WriteContent("Back to top").EndTag();
            w.EndTag(); //li
            w.EndTag(); //ul
            w.EndTag(); //nav
            w.WriteLine();
            w.WriteLine();
        }

        /// <summary>
        ///     Resize the image to the specified width and height.
        /// </summary>
        private static void ResizeImage([NotNull] string srcFullName, int height, [NotNull] string dstPath)
        {
            using (FileStream fs = new FileStream(srcFullName, FileMode.Open)) {
                using (Image image = Image.FromStream(fs)) {
                    double factor = image.Height / (double)height;
                    int width = (int)(image.Width / factor);
                    var destRect = new Rectangle(0, 0, width, height);
                    using (var destImage = new Bitmap(width, height)) {
                        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                        using (var graphics = Graphics.FromImage(destImage)) {
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            using (var wrapMode = new ImageAttributes()) {
                                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                            }
                        }

                        destImage.Save(dstPath, ImageFormat.Png);
                    }
                }
            }
        }

        private static void WriteMenuItem([NotNull] HtmlTextWriter w, [NotNull] string txt, [NotNull] string key, [NotNull] string size)
        {
            w.Li(new {@class = "nav-item"});
            w.A(new {@class = "nav-link", href = "#" + key});
            w.Span(new[] {"data-feather=file", "class=" + size}).WriteContent(txt).EndTag();

            w.EndTag(); //a
            w.EndTag(); //li
        }

        private static void WriteSideNavBar([NotNull] [ItemNotNull] List<FlaResultFileEntry> f1Rfes,
                                            [NotNull] HtmlTextWriter w,
                                            [NotNull] List<int> years)
        {
            w.Nav(new {@class = "col-md-2 d-none d-md-block bg-light sidebar d-print-none"});
            w.Div(new {@class = "sidebar-sticky"});
            w.Ul(new {@class = "nav flex-column"});
            w.WriteLine();
            w.Li(new {@class = "nav-item"});
            w.A(new {@class = "nav-link active", href = "#"});
            w.Span(new[] {"data-feather=home"}).WriteContent("Dashboard").EndTag();
            w.Span(new {@class = "sr-only"}).WriteContent("(current)").EndTag();
            w.EndTag(); //a
            w.EndTag(); //li
            w.WriteLine();
            foreach (var year in years) {
                WriteMenuItem(w, year.ToString(CultureInfo.InvariantCulture), "year" + year, "h4");
                var stages = f1Rfes.Select(x => x.SrcStageStr).Distinct().ToList();
                foreach (var stage in stages) {
                    WriteMenuItem(w, stage, "stage" + CleanStageName(stage), "h5");
                    var sections = f1Rfes.Where(x => x.SrcStageStr == stage).Select(x => x.Section.ToString(CultureInfo.InvariantCulture)).Distinct()
                        .ToList();
                    foreach (var section in sections) {
                        WriteMenuItem(w, section, "section" + CleanStageName(section), "h6");
                    }
                }
            }

            w.EndTag(); //ul
            w.EndTag(); //div
            w.EndTag(); //nav
        }

        private static void WriteYearSection([NotNull] HtmlTextWriter w,
                                             int year,
                                             [NotNull] [ItemNotNull] List<FlaResultFileEntry> f2Fres,
                                             [NotNull] Dictionary<FlaResultFileEntry, string> dstFileNameByRfe,
                                             [NotNull] Dictionary<FlaResultFileEntry, string> dstFileNameSmallByRfe)
        {
            w.Div(new {@class = "row"});
            w.P(new {id = "year" + year, style = "padding-top: 90px;"}).EndTag();
            w.EndTag();
            w.Div(new {@class = "row"});
            w.H1().WriteContent("Year " + year).EndTag();
            w.EndTag(); //div row
            var stages = f2Fres.Select(x => x.SrcStage.ToString()).Distinct().ToList();
            foreach (string stage in stages) {
                w.Div(new {@class = "row"});
                w.P(new {id = "stage" + CleanStageName(stage), style = "padding-top: 90px;"}).EndTag();
                w.EndTag();
                w.Div(new {@class = "row"});
                w.H2().WriteContent("Stage " + stage).EndTag();
                w.EndTag(); //div
                var f3Fres = f2Fres.Where(x => x.SrcStage.ToString() == stage).ToList();
                var sections = f3Fres.Select(x => x.Section.ToString(CultureInfo.InvariantCulture)).Distinct().ToList();
                foreach (var section in sections) {
                    w.Div(new {@class = "row"});
                    w.Hr(new {
                        id = "section" + CleanStageName(section), style = "padding-top: 90px;"
                    }).EndTag();
                    w.H3().WriteContent("Step " + section).EndTag();
                    w.EndTag(); //div
                    var files = f2Fres.Where(x => x.Section == section).ToList();
                    foreach (FlaResultFileEntry entry in files) {
                        w.Div(new {@class = "row"});
                        w.P().WriteContent(entry.FullFilename).EndTag();
                        w.EndTag();
                        w.Div(new {@class = "row"});
                        string dstfnFull = Resources + "/" + dstFileNameByRfe[entry];
                        string dstfnSmall = Resources + "/" + dstFileNameSmallByRfe[entry];
                        w.P();
                        w.A(new {href = dstfnFull, target = "_blank"});
                        w.Img(new {src = dstfnSmall, height = "400"}).EndTag();
                        w.EndTag(); //a
                        w.EndTag(); //p
                        w.EndTag(); //div row
                    }
                }
            }

            //w.EndTag(); //div container
//            w.Div(new {@class = "outer"}).P().WriteContent("some content").Table().Tr().Td().WriteContent("hi").EndTag().EndTag().EndTag().EndTag().EndTag().EndTag();
        }
    }
}