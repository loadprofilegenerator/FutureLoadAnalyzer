using Common.Config;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Logging {
    public interface ILogger {
        void AddStageEntry([NotNull] StageEntry se, [NotNull] RunningConfig config);
        void Debug([NotNull] string message, Stage stage, [NotNull] string name);
        void ErrorM([NotNull] string message, Stage stage, [NotNull] string name);
        void Info([NotNull] string message, Stage stage, [NotNull] string name);
        void Info([NotNull] string message, Stage stage, [NotNull] string name, [CanBeNull] object o);
        void Trace([NotNull] string message, Stage stage, [NotNull] string name);
        void Warning([NotNull] string message, Stage stage, [NotNull] string name);
        void Warning([NotNull] string message, Stage stage, [NotNull] string name, [CanBeNull] object o);
    }
}