using System;
using Common.Config;
using Common.Database;
using Common.Logging;
using JetBrains.Annotations;
using Visualizer.Mapper;

namespace Visualizer {
    public interface IServiceRepository {
        DateTime StartingTime { get; }
        [NotNull]
        PlotMaker PlotMaker { get; }
        [NotNull]
        MapDrawer MapDrawer { get; }
        [NotNull]
        ILogger Logger { get; }
        [NotNull]
        RunningConfig RunningConfig { get; }
        [NotNull]
        Random Rnd { get; }
        [NotNull]
        SqlConnectionPreparer SqlConnectionPreparer { get; }
    }
}