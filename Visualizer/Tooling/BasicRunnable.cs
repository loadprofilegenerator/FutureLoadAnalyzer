using System.Collections.Generic;
using System.Diagnostics;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Visualisation;
using Common;
using Common.Database;
using Common.Steps;
using JetBrains.Annotations;
using Visualizer;

namespace BurgdorfStatistics.Tooling {
    public abstract class BasicRunnable : IBasicRunner
    {
        [CanBeNull] private readonly IVisualizeSlice _visualize;
        [NotNull]
        [ItemNotNull]
        public List<string> DevelopmentStatus { get; } = new List<string>();

        public void LogCall([NotNull] Stopwatch sw)
        {
            Services.MyLogger.AddStageEntry(new Logger.StageEntry(SequenceNumber, MyStage,
                Name, sw.Elapsed.TotalSeconds, ImplementationFinished, GetDevelopmentStatusString(),
                VisualizeSlice?.VisualizerName,FilesCreated,MakeChartFunctionExecuted));
        }

        [NotNull]
        public string GetThisTargetDirectory() => FilenameHelpers.GetTargetDirectory(MyStage, SequenceNumber, Name, Constants.PresentSlice);


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
        [NotNull]
        public string Name { get; }

        [NotNull]
        public MySqlConnection SqlConnection => Services.SqlConnection;

        public int FilesCreated { get; set; }
        public Stage MyStage { get; }
        public int SequenceNumber { get; }
        public Steptype StepType { get; }

        [NotNull]
        public ServiceRepository Services { get; }

        public bool ImplementationFinished { get; }

        [CanBeNull]
        public IVisualizeSlice VisualizeSlice => _visualize;

        public void Log(MessageType type, [NotNull] string message, [CanBeNull] object o = null)
        {
            Services.MyLogger.AddMessage(new LogMessage(type, message, Name, MyStage, o));
        }


        public void Info([NotNull] string message, [CanBeNull] object o = null)
        {
            Services.MyLogger.AddMessage(new LogMessage(MessageType.Info, message, Name, MyStage, o));
        }

        [NotNull]
        public string MakeAndRegisterFullFilename([NotNull] string filename, [NotNull] ScenarioSliceParameters slice)
        {
            FilesCreated++;
            return FilenameHelpers.MakeAndRegisterFullFilenameStatic(filename, Name, "", MyStage, SequenceNumber, Name, slice);
        }
        [NotNull]
        public string MakeAndRegisterFullFilename([NotNull] string filename, [NotNull] string section, [NotNull] string sectionDescription, [NotNull] ScenarioSliceParameters slice)
        {
            FilesCreated++;
            return FilenameHelpers.MakeAndRegisterFullFilenameStatic(filename, section, sectionDescription, MyStage, SequenceNumber, Name, slice);
        }


        protected BasicRunnable([NotNull] string name, Stage stage, int sequenceNumber, Steptype stepType,
                                [NotNull] ServiceRepository services, bool implementationFinished, [CanBeNull] IVisualizeSlice visualize)
        {
            _visualize = visualize;
            Name = name;
            MyStage = stage;
            SequenceNumber = sequenceNumber;
            StepType = stepType;
            Services = services;
            ImplementationFinished = implementationFinished;
        }
    }
}