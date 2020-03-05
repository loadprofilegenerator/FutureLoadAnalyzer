using Common.Steps;
using JetBrains.Annotations;

namespace Common.Logging {
    public static class SLogger {
        [CanBeNull] private static ILogger _logger;

        public static void Debug([NotNull] string message)
        {
            if (_logger == null) {
                throw new FlaException("Logger was null");
            }

            _logger.Debug(message, Stage.Preparation, "Static");
        }

        public static void Error([NotNull] string message)
        {
            if (_logger == null) {
                throw new FlaException("Logger was null");
            }

            _logger.ErrorM(message, Stage.Preparation, "Static");
        }

        public static void Info([NotNull] string message)
        {
            if (_logger == null) {
                throw new FlaException("Logger was null");
            }

            _logger.Info(message, Stage.Preparation, "Static");
        }

        public static void Init([NotNull] ILogger logger)
        {
            _logger = logger;
        }
    }
}