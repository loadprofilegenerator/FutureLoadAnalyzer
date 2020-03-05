using System.Globalization;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Logging {
    public class StageEntry {
        public StageEntry(int stageNumber, Stage stage, [NotNull] string name, double seconds, bool implementationFinished, [NotNull] string developmentStatus, [CanBeNull] string sliceVisualizerName,
                          int filesCreated, bool makeChartFunctionExecuted)
        {
            StageNumber = stageNumber;
            Stage = stage;
            Name = name;
            Seconds = seconds;
            ImplementationFinished = implementationFinished;
            DevelopmentStatus = developmentStatus;
            if (sliceVisualizerName != null) {
                IsSliceVisualizerSet = true;
                SliceVisualizerName = sliceVisualizerName;
            }

            FilesCreated = filesCreated;
            MakeChartFunctionExecuted = makeChartFunctionExecuted;
        }

        [NotNull]
        public string DevelopmentStatus { get; }

        public int FilesCreated { get; }
        public bool ImplementationFinished { get; set; }
        public bool IsSliceVisualizerSet { get; }

        [NotNull]
        public string Key => ((int)Stage).ToString("00", CultureInfo.InvariantCulture) + "#" + StageNumber.ToString("0000", CultureInfo.InvariantCulture);

        public bool MakeChartFunctionExecuted { get; }

        [NotNull]
        public string Name { get; }

        public double Seconds { get; }

        [CanBeNull]
        public string SliceVisualizerName { get; }

        public Stage Stage { get; }

        public int StageNumber { get; }
    }
}