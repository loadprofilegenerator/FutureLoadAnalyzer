using System.Collections.Generic;
using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer.Visualisation {
    public interface IVisualizeSlice {
        [NotNull]
        [ItemNotNull]
        List<string> DevelopmentStatus { get; }

        [NotNull]
        string VisualizerName { get; }

        void MakeVisualization([NotNull] ScenarioSliceParameters slice, [NotNull] IBasicRunner runnable);
    }
}