using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.Visualisation;

namespace FutureLoadAnalyzerLib.Tooling.Steps {
    public abstract class BasicRunnable : BasicLoggable, IBasicRunner
    {
        [CanBeNull] private readonly IVisualizeSlice _visualize;
        [NotNull]
        [ItemNotNull]
        public List<string> DevelopmentStatus { get; } = new List<string>();

        public void ClearTargetDirectory([NotNull] ScenarioSliceParameters slice)
        {
            var fullpath = FilenameHelpers.GetTargetDirectory(MyStage,
                SequenceNumber, Name, slice, Services.RunningConfig);
            if (Directory.Exists(fullpath)) {
                try {
                    DirectoryInfo di = new DirectoryInfo(fullpath);
                    var files = di.GetFiles("*.*", SearchOption.AllDirectories);
                    foreach (var fileInfo in files) {
                        fileInfo.Delete();
                    }
                    di.Delete(true);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    SLogger.Error(e.Message);
                }
            }

            if (!Directory.Exists(fullpath)) {
                Directory.CreateDirectory(fullpath);
                Thread.Sleep(250);
            }
        }
        public void LogCall([NotNull] Stopwatch sw)
        {
            Services.Logger.AddStageEntry(new StageEntry(SequenceNumber, MyStage,
                Name, sw.Elapsed.TotalSeconds, ImplementationFinished, GetDevelopmentStatusString(),
                VisualizeSlice?.VisualizerName,FilesCreated,MakeChartFunctionExecuted),Services.RunningConfig);
        }

        [NotNull]
        public string GetThisTargetDirectory()
            => FilenameHelpers.GetTargetDirectory(MyStage, SequenceNumber,
                Name, Constants.PresentSlice,Services.RunningConfig);


        [NotNull]
        public string GetDevelopmentStatusString()
        {
            string s = string.Join(", ", DevelopmentStatus);
            if (_visualize != null) {
                foreach (string visualizeDevelopmentStatu in _visualize.DevelopmentStatus) {
                    s += ", " + _visualize.VisualizerName + ": " + visualizeDevelopmentStatu;
                }
            }
            return s;
        }

        public bool MakeChartFunctionExecuted { get; set; }
        public int FilesCreated { get; set; }
        public int SequenceNumber { get; }
        public Steptype StepType { get; }

        [NotNull]
        public ServiceRepository Services { get; }

        public bool ImplementationFinished { get; }

        [CanBeNull]
        public IVisualizeSlice VisualizeSlice => _visualize;

        [NotNull]
        public string CombineForRaw([NotNull] string filename)
        {
            return Path.Combine(Services.RunningConfig.Directories.BaseRawDirectory, filename);
        }
        [NotNull]
        public string CombineForFlaSettings([NotNull] string filename)
        {
            return Path.Combine(Services.RunningConfig.Directories.BaseUserSettingsDirectory, filename);
        }
        [NotNull]
        public string MakeAndRegisterFullFilename([NotNull] string filename, [NotNull] ScenarioSliceParameters slice, bool replaceSpaces = true)
        {
            FilesCreated++;
            return FilenameHelpers.MakeAndRegisterFullFilenameStatic(
                filename,  MyStage, SequenceNumber,
                Name, slice,Services.RunningConfig, replaceSpaces);
        }
       /* [NotNull]
        public string MakeAndRegisterFullFilename([NotNull] string filename, [NotNull] ScenarioSliceParameters slice)
        {
            FilesCreated++;
            return FilenameHelpers.MakeAndRegisterFullFilenameStatic(filename,  MyStage, SequenceNumber, Name, slice,
                Services.RunningConfig, true);
        }
        */
        public void SaveToArchiveDirectory([NotNull] string fn,RelativeDirectory relativeDir,[NotNull] ScenarioSliceParameters slice )
        {
            if (slice.SmartGridEnabled && slice.DstYear != 2050) {
                return;
            }
            var dstDir = GetResultArchiveDirectory(slice, Services.StartingTime, Services.RunningConfig, relativeDir, null);
            FileInfo fi1 = new FileInfo(fn);
            fi1.CopyTo(Path.Combine(dstDir, fi1.Name),true);
        }


        public void SaveToPublicationDirectory([NotNull] string fn,  [NotNull] ScenarioSliceParameters slice, [NotNull] string chapter)
        {
            var dstDir = GetResultArchiveDirectory(slice, Services.StartingTime, Services.RunningConfig, RelativeDirectory.Abschlussbericht, chapter);
            FileInfo fi1 = new FileInfo(fn);
            fi1.CopyTo(Path.Combine(dstDir, fi1.Name), true);
        }
        [NotNull]
        public static string GetResultArchiveDirectory([NotNull] ScenarioSliceParameters slice, DateTime startingTime, [NotNull] RunningConfig config,
                                                       RelativeDirectory relativeDir, [CanBeNull] string chapter)
        {
            string date = FilenameHelpers.CleanFileName(startingTime.ToString("yyyy-MM-dd"));
            string scenario = slice.GetFileName();
            if (slice.SmartGridEnabled) {
                scenario += "smart";
            }
            string resultArchiveDirectory = Path.Combine(config.Directories.ResultStorageDirectory, date, scenario,
                relativeDir.ToString());
            if (chapter != null) {
                resultArchiveDirectory = Path.Combine(config.Directories.ResultStorageDirectory, "Abschlussbericht",date,
                    "Kapitel " + chapter,
                    scenario);
            }
            if (!Directory.Exists(resultArchiveDirectory)) {
                Directory.CreateDirectory(resultArchiveDirectory);
                Thread.Sleep(250);
            }
            return resultArchiveDirectory;
        }

        protected BasicRunnable([NotNull] string name, Stage stage, int sequenceNumber, Steptype stepType,
                                [NotNull] ServiceRepository services, bool implementationFinished,
                                [CanBeNull] IVisualizeSlice visualize): base(services.Logger,stage,name)
        {
            _visualize = visualize;
            SequenceNumber = sequenceNumber;
            StepType = stepType;
            Services = services;
            ImplementationFinished = implementationFinished;
        }
    }
}