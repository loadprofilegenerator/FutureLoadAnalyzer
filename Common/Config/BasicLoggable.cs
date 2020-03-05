using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Config {
    public class BasicLoggable {
        [NotNull] private readonly ILogger _logger;

        public BasicLoggable([NotNull] ILogger logger, Stage myStage, [NotNull] string name)
        {
            _logger = logger;
            MyStage = myStage;
            if (string.IsNullOrWhiteSpace(name)) {
                throw new FlaException("name was empty");
            }

            Name = name;
        }

        [NotNull]
        public ILogger MyLogger => _logger;

        public Stage MyStage { get; }

        [NotNull]
        public string Name { get; }

        public void Debug([NotNull] string message)
        {
            _logger.Debug(message, MyStage, Name);
        }

        public void Error([NotNull] string message)
        {
            _logger.ErrorM(message, MyStage, Name);
        }

        public void Info([NotNull] string message)
        {
            _logger.Info(message, MyStage, Name);
        }

        public void Info([NotNull] string message, [CanBeNull] object o)
        {
            _logger.Info(message, MyStage, Name, o);
        }

        public void Trace([NotNull] string message)
        {
            _logger.Trace(message, MyStage, Name);
        }

        public void Warning([NotNull] string message)
        {
            _logger.Warning(message, MyStage, Name);
        }

        public void Warning([NotNull] string message, [CanBeNull] object o)
        {
            _logger.Warning(message, MyStage, Name, o);
        }
    }
}