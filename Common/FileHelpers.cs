using System.IO;
using System.Linq;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;

namespace Common {
    public static class FileHelpers {
#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void CopyRec([NotNull] string src, [NotNull] string dst, [NotNull] ILogger logger, bool deleteExtraDirectories)
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
            logger.Info("Copying from " + src + " to " + dst, Stage.Preparation, nameof(CopyRec));
            DirectoryInfo srcInfo = new DirectoryInfo(src);
            DirectoryInfo dstInfo = new DirectoryInfo(dst);
            int filecount = 0;
            long fileSize = 0;
            if (!Directory.Exists(dst)) {
                Directory.CreateDirectory(dst);
            }

            CopyFilesRecursively(srcInfo, dstInfo, ref filecount, ref fileSize, logger, deleteExtraDirectories);
            logger.Info("Copied " + filecount + " with a total of " + fileSize + " bytes", Stage.Preparation, nameof(CopyRec));
        }

        private static void CopyFilesRecursively([NotNull] DirectoryInfo source,
                                                 [NotNull] DirectoryInfo target,
                                                 ref int filecount,
                                                 ref long filesize,
                                                 [NotNull] ILogger logger,
                                                 bool deleteExtraDirectories)
        {
            var targetDirs = target.GetDirectories().ToList();
            foreach (DirectoryInfo dir in source.GetDirectories()) {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), ref filecount, ref filesize, logger, deleteExtraDirectories);
                var matchingTargetDir = targetDirs.FirstOrDefault(x => x.Name == dir.Name);
                if (matchingTargetDir != null) {
                    targetDirs.Remove(matchingTargetDir);
                }
            }

            foreach (DirectoryInfo extraDir in targetDirs) {
                if (deleteExtraDirectories) {
                    logger.Info("Extra directory " + extraDir.FullName + ", deleting", Stage.Preparation, nameof(CopyFilesRecursively));
                    extraDir.Delete(true);
                }
                else {
                    logger.Info("Extra directory " + extraDir.FullName + ", ignoring", Stage.Preparation, nameof(CopyFilesRecursively));
                }
            }

            var targetFiles = target.GetFiles().ToList();
            foreach (FileInfo file in source.GetFiles()) {
                string targetpath = Path.Combine(target.FullName, file.Name);
                if (!File.Exists(targetpath)) {
                    file.CopyTo(targetpath);
                    filecount++;
                    filesize += file.Length;
                }
                else {
                    FileInfo targetInfo = new FileInfo(targetpath);
                    var matchingTargetFile = targetFiles.FirstOrDefault(x => x.Name == file.Name);
                    if (matchingTargetFile != null) {
                        targetFiles.Remove(matchingTargetFile);
                    }

                    if (IsFileChanged(file, targetInfo)) {
                        logger.Info("File changed: " + file, Stage.Preparation, nameof(CopyFilesRecursively));
                        file.CopyTo(targetpath, true);
                        filecount++;
                        filesize += file.Length;
                    }
                }
            }

            foreach (FileInfo info in targetFiles) {
                logger.Info("Deleted " + info.Name, Stage.Preparation, nameof(CopyFilesRecursively));
                info.Delete();
            }
        }

        private static bool IsFileChanged([NotNull] FileInfo src, [NotNull] FileInfo dst)
        {
            if (src.Length != dst.Length) {
                return true;
            }

            if (src.LastWriteTime != dst.LastWriteTime) {
                return true;
            }

            return false;
        }
    }
}