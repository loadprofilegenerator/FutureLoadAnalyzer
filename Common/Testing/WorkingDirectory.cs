using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Common.Config;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;

namespace Common.Testing {
    public class WorkingDirectory : BasicLoggable {
        public WorkingDirectory([NotNull] Logger logger, [NotNull] RunningConfig config) : base(logger, Stage.Preparation, nameof(WorkingDirectory))
        {
            string baseDir = Path.Combine(config.Directories.BaseProcessingDirectory, "unittests");
            string callingMethod = GetCallingMethodAndClass();
            Info("Base dir: " + baseDir);
            Info("Calling Method: " + callingMethod);
            Dir = Path.Combine(baseDir, FilenameHelpers.CleanFileName(callingMethod));
            Info("Used Directory: " + Dir);
            DirDi = new DirectoryInfo(Dir);
            if (DirDi.Exists) {
                try {
                    DirDi.Delete(true);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    logger.ErrorM(ex.Message, Stage.Preparation, nameof(WorkingDirectory));
                }

                Thread.Sleep(250);
            }

            DirDi.Create();
            Thread.Sleep(250);
        }

        [NotNull]
        public string Dir { get; }

        [NotNull]
        public DirectoryInfo DirDi { get; }

        public void CleanAndCreate()
        {
            if (DirDi.Exists) {
                try {
                    DirDi.Delete(true);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Info(ex.Message);
                }
            }

            DirDi.Create();
            Thread.Sleep(250);
        }

        [NotNull]
        public string Combine([NotNull] string name) => Path.Combine(Dir, name);

        [NotNull]
        public static string GetCallingMethodAndClass()
        {
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
            if (frames == null) {
                throw new FlaException("frames was null");
            }

            var method = frames[3].GetMethod();

            if (method.DeclaringType == null) {
                throw new FlaException("DeclaringType was null");
            }

            return method.DeclaringType.Name + "." + method.Name;
        }
    }
}