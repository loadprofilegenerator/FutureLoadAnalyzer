using System;
using BurgdorfStatistics.Logging;
using Common;
using Common.Database;
using Data;
using JetBrains.Annotations;
using Visualizer;
using Visualizer.Mapper;

namespace BurgdorfStatistics.Tooling {
    [UsedImplicitly]
    public class ServiceRepository : IServiceRepository {
        public ServiceRepository([NotNull] PlotMaker plotMaker, [NotNull] MapDrawer mapDrawer, [NotNull] MySqlConnection sqlConnection, [NotNull] Logger logger,
                                 [NotNull] RunningConfig runningConfig, [NotNull] Random rnd)
        {
            PlotMaker = plotMaker;
            MapDrawer = mapDrawer;
            SqlConnection = sqlConnection;
            Logger = logger;
            MyLogger = logger;
            RunningConfig = runningConfig;
            Rnd = rnd;
        }

        [NotNull]
        public PlotMaker PlotMaker { get; }

        [NotNull]
        public MapDrawer MapDrawer { get; }

        [NotNull]
        public MySqlConnection SqlConnection { get; }

        [NotNull]
        public ILogger Logger { get; }

        [NotNull]
        public Logger MyLogger { get; }

        [NotNull]
        public RunningConfig RunningConfig { get; }

        [NotNull]
        public Random Rnd { get; }
    }
}