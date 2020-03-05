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

namespace Visualizer.Visualisation {
    public abstract class VisualisationBase : BasicLoggable, IVisualizeSlice {
        [CanBeNull] private readonly IServiceRepository _services;
        [NotNull] private string _name;
        private int _sequenceNumber;

        protected VisualisationBase([NotNull] string visualizerName, [NotNull] IServiceRepository services, Stage myStage) : base(services.Logger,
            myStage,
            visualizerName)
        {
            VisualizerName = visualizerName;
            _name = "";
            _services = services;
        }


        public int SequenceNumber => _sequenceNumber;


        [NotNull]
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public IServiceRepository Services => _services ?? throw new FlaException("Services was null");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
        [NotNull]
        [ItemNotNull]
        public List<string> DevelopmentStatus { get; } = new List<string>();

        public void MakeVisualization([NotNull] ScenarioSliceParameters slice, [NotNull] IBasicRunner runnable)
        {
            _name = runnable.Name;
            _sequenceNumber = runnable.SequenceNumber;
            // ReSharper disable once ReplaceWithSingleAssignment.False
            var isPresent = false;
            if (slice.DstScenario == Constants.PresentSlice.DstScenario && slice.DstYear == Constants.PresentSlice.DstYear) {
                isPresent = true;
            }

            ThreadClass tc = new ThreadClass("Visualisation_" + _name, () => MakeVisualization(slice, isPresent), Services.Logger);
            if (tc == null) {
                throw new FlaException("huch?");
            }
        }

        public string VisualizerName { get; }

        [NotNull]
        public static string GetResultArchiveDirectory([NotNull] ScenarioSliceParameters slice,
                                                       DateTime startingTime,
                                                       [NotNull] RunningConfig config,
                                                       RelativeDirectory relativeDir,
                                                       [CanBeNull] string chapter)
        {
            string date = FilenameHelpers.CleanFileName(startingTime.ToString("yyyy-MM-dd"));
            string scenario = slice.GetFileName();
            if (slice.SmartGridEnabled) {
                scenario += "smart";
            }

            string resultArchiveDirectory = Path.Combine(config.Directories.ResultStorageDirectory, date, scenario, relativeDir.ToString());
            if (chapter != null) {
                resultArchiveDirectory = Path.Combine(config.Directories.ResultStorageDirectory,
                    "Abschlussbericht",
                    date,
                    "Kapitel " + chapter,
                    scenario);
            }

            if (!Directory.Exists(resultArchiveDirectory)) {
                Directory.CreateDirectory(resultArchiveDirectory);
                Thread.Sleep(250);
            }

            return resultArchiveDirectory;
        }

        public void SaveToPublicationDirectory([NotNull] string fn, [NotNull] ScenarioSliceParameters slice, [NotNull] string chapter)
        {
            var dstDir = GetResultArchiveDirectory(slice, Services.StartingTime, Services.RunningConfig, RelativeDirectory.Abschlussbericht, chapter);
            FileInfo fi1 = new FileInfo(fn);
            fi1.CopyTo(Path.Combine(dstDir, fi1.Name), true);
        }

        [NotNull]
        protected string MakeAndRegisterFullFilename([NotNull] string filename, [NotNull] ScenarioSliceParameters slice) =>
            FilenameHelpers.MakeAndRegisterFullFilenameStatic(filename, MyStage, _sequenceNumber, _name, slice, Services.RunningConfig, true);


        protected abstract void MakeVisualization([NotNull] ScenarioSliceParameters slice, bool isPresent);

        private class ThreadClass : BasicLoggable {
            public ThreadClass([NotNull] string name, [NotNull] Action func, [NotNull] ILogger logger) : base(logger, Stage.Plotting, name)
            {
                Func = func;
                var t = ThreadProvider.Get().MakeThreadAndStart(SaveExecute, Name, true);
                Thread = t;
            }


            [CanBeNull]
            public Exception Ex { get; set; }

            [NotNull]
            public Action Func { get; }


            [NotNull]
            public Thread Thread { get; }

            public void SaveExecute()
            {
                try {
                    var sw = new Stopwatch();
                    sw.Start();
                    Func();
                    sw.Stop();
                    Info("Executing " + Name + " took " + sw.ElapsedMilliseconds + " ms");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Ex = ex;
                }
            }
        }
    }
}