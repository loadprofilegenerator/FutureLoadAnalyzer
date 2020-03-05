using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Common.Config;
using Common.Steps;
using JetBrains.Annotations;

namespace Common {
    public static class FilenameHelpers {
        [NotNull] private static readonly Dictionary<string, string> _usedFileNames = new Dictionary<string, string>();

        [NotNull]
        public static string CleanFileName([NotNull] string oldname)
        {
            var newname = oldname;
            var forbiddenchars = Path.GetInvalidFileNameChars();
            return forbiddenchars.Aggregate(newname, (current, forbiddenchar) => current.Replace(forbiddenchar, ' '));
        }

        [NotNull]
        public static string CleanUmlaute([NotNull] string s) => s.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue");

        [NotNull]
        public static string GetTargetDirectory(Stage stage,
                                                int sequenceNumber,
                                                [CanBeNull] string className,
                                                [NotNull] ScenarioSliceParameters slice,
                                                [NotNull] RunningConfig config)
        {
            string dstPath;
            string baseDir = config.Directories.BaseProcessingDirectory;
            if (stage == Stage.Testing) {
                baseDir = config.Directories.UnitTestingDirectory;
            }

            if (sequenceNumber == -1) {
                dstPath = Path.Combine(baseDir, slice.DstScenario.ToString(), slice.DstYear.ToString(CultureInfo.InvariantCulture));
            }
            else {
                var stagenum = (int)stage;
                var subdirName = stagenum.ToString("D2", CultureInfo.InvariantCulture) + "-" + stage + " # " +
                                 sequenceNumber.ToString("D3", CultureInfo.InvariantCulture) + "-" + className;
                dstPath = Path.Combine(baseDir, slice.DstScenario.ToString(), slice.DstYear.ToString(CultureInfo.InvariantCulture), subdirName);
                if (slice.SmartGridEnabled) {
                    dstPath = Path.Combine(dstPath, "Smart");
                }
            }

            if (!Directory.Exists(dstPath)) {
                Directory.CreateDirectory(dstPath);
                Thread.Sleep(250);
            }

            return dstPath;
        }

        [NotNull]
        public static string MakeAndRegisterFullFilenameStatic([NotNull] string filename,
                                                               Stage stage,
                                                               int sequenceNumber,
                                                               [NotNull] string name,
                                                               [NotNull] ScenarioSliceParameters slice,
                                                               [NotNull] RunningConfig config,
                                                               bool replaceSpaces)
        {
            if (replaceSpaces) {
                filename = filename.Replace(" ", "");
            }

            var fullpath = GetTargetDirectory(stage, sequenceNumber, name, slice, config);
            if (!Directory.Exists(fullpath)) {
                Directory.CreateDirectory(fullpath);
                Thread.Sleep(500);
            }

            var fullName = Path.Combine(fullpath, filename);
            if (_usedFileNames.ContainsKey(fullName)) {
                throw new FlaException("File already registered: " + fullName + " @ this location:\n" + _usedFileNames[fullName] +
                                       " \n--------------------------\n");
            }

            StackTrace t = new StackTrace();
            _usedFileNames.Add(fullName, t.ToString());
            return fullName;
        }
    }
}