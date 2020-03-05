using System;
using Common.Config;
using Common.Database;
using Common.Logging;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.Mapper;

namespace FutureLoadAnalyzerLib.Tooling {
    [UsedImplicitly]
    public class ServiceRepository : IServiceRepository {
        public ServiceRepository([NotNull] PlotMaker plotMaker,
                                 [NotNull] MapDrawer mapDrawer,
                                 [NotNull] Logger logger,
                                 [NotNull] RunningConfig runningConfig,
                                 [NotNull] Random rnd)
        {
            PlotMaker = plotMaker;
            MapDrawer = mapDrawer;
            Logger = logger;
            MyLogger = logger;
            RunningConfig = runningConfig;
            Rnd = rnd;
            SqlConnectionPreparer = new SqlConnectionPreparer(runningConfig);
            StartingTime = DateTime.Now;
        }

        [NotNull]
        public Logger MyLogger { get; }

        [NotNull]
        public ILogger Logger { get; }

        [NotNull]
        public MapDrawer MapDrawer { get; }

        [NotNull]
        public PlotMaker PlotMaker { get; }

        [NotNull]
        public Random Rnd { get; }

        [NotNull]
        public RunningConfig RunningConfig { get; }

        [NotNull]
        public SqlConnectionPreparer SqlConnectionPreparer { get; }

        public DateTime StartingTime { get; }
    }
}