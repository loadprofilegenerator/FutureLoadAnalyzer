using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Common.Config;
using Common.Database;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Xabe;
using Xunit.Abstractions;

namespace Common.Logging {
#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class Logger : ILogger, IDisposable {
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
#pragma warning restore CA1063 // Implement IDisposable Correctly
        [NotNull] private readonly RunningConfig _config;

        [ItemNotNull] [NotNull] private readonly List<LogMessage> _logMessagesForDB = new List<LogMessage>();
        [CanBeNull] private readonly ITestOutputHelper _unittestoutput;
        [NotNull] private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        [NotNull] private readonly Thread _myThread;
        public Logger([CanBeNull] ITestOutputHelper unittestoutput, [NotNull] RunningConfig config)
        {
            config.CheckInitalisation();
            _unittestoutput = unittestoutput;
            _config = config;
            _myThread = ThreadProvider.Get().MakeThreadAndStart(SafeRun, "Logger");
            SLogger.Init(this);
        }

        [NotNull] [ItemNotNull] private readonly BlockingCollection<LogMessage> _logMessages = new BlockingCollection<LogMessage>(1000);
        private void SafeRun()
        {
            try {
                Console.WriteLine("Starting Logger Thread");
                //main thread
                while (!_cancellationTokenSource.Token.IsCancellationRequested) {
                    try {
                        var message = _logMessages.Take(_cancellationTokenSource.Token);
                        ProcessMessage(message);
                    }
                    catch (OperationCanceledException) {
                        Console.WriteLine("Stopping the logger.");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Console.WriteLine(ex.GetType().FullName + ":" + ex.Message + " " + ex.StackTrace);
                    }
                }

                Console.WriteLine("Shutting down Logger Thread, " + _logMessages.Count + " entries left in queue");
                //clean up queue
                while (_logMessages.Count > 0) {
                    var prosumer = _logMessages.Take();
                    ProcessMessage(prosumer);
                }

                Console.WriteLine("Finished Shutting down Logger Thread");
            }
            catch (OperationCanceledException) {
                Console.WriteLine("Stopping the logger.");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                Console.WriteLine(ex.GetType().FullName + ":" + ex.Message + " " + ex.StackTrace);
            }
        }

        [ItemNotNull]
        [NotNull]
        public static ObservableCollection<string> LogMessagesForWindow { get; } = new ObservableCollection<string>();
        public void Warning([NotNull] string message, Stage stage, [NotNull] string name)
        {
            _logMessages.Add(new LogMessage(MessageType.Warning, message, name, stage, null));
        }

        public void Warning(string message, Stage stage, [NotNull] string name, object o)
        {
            _logMessages.Add(new LogMessage(MessageType.Warning, message, name, stage, o));
        }

        public void AddStageEntry([NotNull] StageEntry se, [NotNull] RunningConfig config)
        {
            if (!config.MakeStageEntries) {
                return;
            }

            Console.WriteLine("Config in stage: " + JsonConvert.SerializeObject(config, Formatting.Indented));
            if (!string.IsNullOrWhiteSpace(se.DevelopmentStatus)) {
                se.ImplementationFinished = false;
            }

            // ReSharper disable once InconsistentlySynchronizedField
            string stagePath = Path.Combine(_config.Directories.BaseProcessingDirectory, "stages.xlsx");
            string lockPath = stagePath + ".lock";
            Random rnd = new Random();
            try {
                using (ILock fileLock = new FileLock(lockPath)) {
                    try {
                        bool lockAquired = false;
                        while (!lockAquired) {
                            try {
                                fileLock.TryAcquire(TimeSpan.FromSeconds(5));
                                lockAquired = true;
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                                Console.WriteLine("Locking failed for file: " + stagePath + ": " + ex.Message);
                                Thread.Sleep(rnd.Next(500));
                            }
                        }

                        WriteXlsFileContent(stagePath, se);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex1) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Console.WriteLine("trying to log result file, exception : " + ex1.Message);
                    }
                    finally {
                        try {
                            fileLock.Dispose();
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (Exception x) {
#pragma warning restore CA1031 // Do not catch general exception types
                            Console.WriteLine("failed to release lock, exception : " + x.Message);
                        }
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning restore CA1031 // Do not catch general exception types
                Console.WriteLine("Locking release failed for file: " + stagePath + ": " + ex.Message);
                Thread.Sleep(rnd.Next(500));
            }
        }

        private static void WriteXlsFileContent([NotNull] string stagePath, [NotNull] StageEntry se)
        {
            var existingFile = new FileInfo(stagePath);
            using (var package = new ExcelPackage(existingFile)) {
                //get the first worksheet in the workbook
                if (package.Workbook.Worksheets.Count == 0) {
                    package.Workbook.Worksheets.Add("Progress");
                }

                if (package.Workbook?.Worksheets[1]?.Cells[1, 1]?.Value?.ToString() != "Key") {
                    var ws = package.Workbook.Worksheets[1];
                    ws.View.FreezePanes(2, 1);
                    int headerCol = 1;
                    ws.Column(headerCol).Width = 15;
                    ws.Cells[1, headerCol++].Value = "Key";
                    ws.Cells[1, headerCol++].Value = "Last Execution";
                    ws.Cells[1, headerCol++].Value = "StageNumber";
                    ws.Cells[1, headerCol++].Value = "Name";
                    ws.Cells[1, headerCol++].Value = "Duration [s]";
                    ws.Cells[1, headerCol++].Value = "Files Created";
                    ws.Cells[1, headerCol++].Value = "MakeChartFunction executed";
                    ws.Cells[1, headerCol++].Value = "Implementation Finished";
                    ws.Cells[1, headerCol++].Value = "MakeChartFunctionExecuted";
                    ws.Cells[1, headerCol++].Value = "Is a slice visualizer used";
                    ws.Cells[1, headerCol++].Value = "Name of Slice Visualizer";
                    // ReSharper disable once RedundantAssignment
                    ws.Cells[1, headerCol++].Value = "Todos";
                }

                var worksheet = package.Workbook.Worksheets[1];
                var rowCount = worksheet.Dimension.End.Row;
                var targetRow = 0;
                for (var row = 1; row <= rowCount; row++) {
                    if (worksheet.Cells[row, 1].Value.ToString() == se.Key) {
                        targetRow = row;
                        break;
                    }
                }

                if (targetRow == 0) {
                    targetRow = rowCount + 1;
                    worksheet.Cells[targetRow, 1].Value = se.Key;
                }

                int col = 2;
                worksheet.Cells[targetRow, col++].Value = se.Stage.ToString();
                worksheet.Cells[targetRow, col++].Value = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
                worksheet.Cells[targetRow, col++].Value = se.StageNumber;
                worksheet.Cells[targetRow, col++].Value = se.Name;
                worksheet.Cells[targetRow, col++].Value = se.Seconds;
                worksheet.Cells[targetRow, col++].Value = se.FilesCreated;
                worksheet.Cells[targetRow, col++].Value = se.ImplementationFinished;
                worksheet.Cells[targetRow, col++].Value = se.MakeChartFunctionExecuted;
                worksheet.Cells[targetRow, col++].Value = se.IsSliceVisualizerSet;
                worksheet.Cells[targetRow, col++].Value = se.SliceVisualizerName;
                // ReSharper disable once RedundantAssignment
                worksheet.Cells[targetRow, col++].Value = se.DevelopmentStatus;
                worksheet.Row(targetRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                if (se.ImplementationFinished) {
                    worksheet.Row(targetRow).Style.Fill.BackgroundColor.SetColor(0, 64, 250, 200);
                }
                else {
                    worksheet.Row(targetRow).Style.Fill.BackgroundColor.SetColor(0, 250, 200, 64);
                }

                package.Save();
            }
        }

        public void Info([NotNull] string message, Stage stage, [NotNull] string name)
        {
            _logMessages.Add(new LogMessage(MessageType.Info, message, name, stage, null));
        }

        public void Info(string message, Stage stage, [NotNull] string name, object o)
        {
            _logMessages.Add(new LogMessage(MessageType.Info, message, name, stage, o));
        }

        public void Debug(string message, Stage stage, [NotNull] string name)
        {
            _logMessages.Add(new LogMessage(MessageType.Debug, message, name, stage, null));
        }

        public void Trace(string message, Stage stage, [NotNull] string name)
        {
            _logMessages.Add(new LogMessage(MessageType.Trace, message, name, stage, null));
        }

        public void ErrorM([NotNull] string message, Stage stage, [NotNull] string name)
        {
            _logMessages.Add(new LogMessage(MessageType.Error, message, name, stage, null));
        }

        public void SaveToDatabase([NotNull] ScenarioSliceParameters slice)
        {
            lock (_logMessagesForDB) {
                var stages = _logMessagesForDB.Select(x => x.DstStage).Distinct().ToList();
                foreach (var stage in stages) {
                    var onestage = _logMessagesForDB.Where(x => x.DstStage == stage).ToList();
                    SqlConnectionPreparer ms = new SqlConnectionPreparer(_config);
                    var db = ms.GetDatabaseConnection(stage, slice);
                    db.CreateTableIfNotExists<LogMessage>();
                    db.BeginTransaction();
                    foreach (var logMessage in onestage) {
                        db.Save(logMessage);
                        _logMessagesForDB.Remove(logMessage);
                    }

                    db.CompleteTransaction();
                }
            }
        }

        public void FinishSavingEverything()
        {
            _cancellationTokenSource.Cancel();
            _myThread.Join();
        }

        private void ProcessMessage([NotNull] LogMessage lm)
        {
            if (_unittestoutput != null && lm.MessageType < MessageType.Debug) {
                _unittestoutput.WriteLine(lm.Message);
            }

            if (lm.MessageType < MessageType.Debug) {
                Console.WriteLine(lm.Message);
            }

            lock (_logMessagesForDB) {
                _logMessagesForDB.Add(lm);
            }
        }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            if (!_cancellationTokenSource.IsCancellationRequested) {
                FinishSavingEverything();
            }

            _cancellationTokenSource.Dispose();
            _logMessages.Dispose();
        }
    }
}