using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Common.Database;
using Common.Steps;
using Data;
using JetBrains.Annotations;
using OfficeOpenXml;
using Xunit.Abstractions;

namespace BurgdorfStatistics.Logging {
    public class Logger : ILogger {
        [CanBeNull] private readonly ITestOutputHelper _unittestoutput;
        public Logger([CanBeNull] ITestOutputHelper unittestoutput) => _unittestoutput = unittestoutput;

        [ItemNotNull] [NotNull] private readonly List<LogMessage> _logMessagesForDB = new List<LogMessage>();

        [ItemNotNull]
        [NotNull]
        public static ObservableCollection<string> LogMessagesForWindow { get; } = new ObservableCollection<string>();

        [CanBeNull]
        public static MainWindow Window { get; set; }
        public void Debug([NotNull] string message)
        {
            AddMessage(new LogMessage(MessageType.Debug, message, "", Stage.Unknown, null));
        }
        public void Info([NotNull] string message)
        {
            AddMessage(new LogMessage(MessageType.Info, message, "", Stage.Unknown, null));
        }


        public void Error([NotNull] string message)
        {
            AddMessage(new LogMessage(MessageType.Error, message, "", Stage.Unknown, null));
        }

        [NotNull] private readonly object _writingLock = new object();
        [NotNull] public static readonly string StagePath = "V:\\BurgdorfStatistics\\stages.xlsx";

        public class StageEntry {
            public StageEntry(int stageNumber, Stage stage, [NotNull] string name, double seconds, bool implementationFinished, [NotNull] string developmentStatus,
                              [CanBeNull] string sliceVisualizerName, int filesCreated, bool makeChartFunctionExecuted)
            {
                StageNumber = stageNumber;
                Stage = stage;
                Name = name;
                Seconds = seconds;
                ImplementationFinished = implementationFinished;
                DevelopmentStatus = developmentStatus;
                if (sliceVisualizerName != null) {
                    IsSliceVisualizerSet = true;
                    SliceVisualizerName = sliceVisualizerName;
                }

                FilesCreated = filesCreated;
                MakeChartFunctionExecuted = makeChartFunctionExecuted;
            }
            public bool MakeChartFunctionExecuted { get; }
            public bool IsSliceVisualizerSet { get; }
            public int FilesCreated { get; }
            [CanBeNull]
            public string SliceVisualizerName { get; }

            public int StageNumber { get; }
            public Stage Stage { get; }
            [NotNull]
            public string Name { get; }
            public double Seconds { get; }
            public bool ImplementationFinished { get; set; }

            [NotNull]
            public string Key => ((int)Stage).ToString("00") + "#" + StageNumber.ToString("0000");

            [NotNull]
            public string DevelopmentStatus { get; }
        }

        public void AddStageEntry([NotNull] StageEntry se)
        {
            if (!string.IsNullOrWhiteSpace(se.DevelopmentStatus)) {
                se.ImplementationFinished = false;
            }
            var existingFile = new FileInfo(StagePath);
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
                worksheet.Row(targetRow).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                if (se.ImplementationFinished) {
                    worksheet.Row(targetRow).Style.Fill.BackgroundColor.SetColor(0, 64, 250, 200);
                }
                else {
                    worksheet.Row(targetRow).Style.Fill.BackgroundColor.SetColor(0, 250, 200, 64);
                }

                package.Save();
            }
        }

        public void AddMessage([NotNull] LogMessage lm)
        {
            if (_unittestoutput != null) {
                _unittestoutput.WriteLine(lm.Message);
            }

            if (lm.MessageType == MessageType.GeneralProgress) {
                lock (_writingLock) {
                    using (var sw = new StreamWriter(StagePath, true)) {
                        sw.WriteLine(lm.Message);
                    }
                }
            }

            if (lm.MessageType < MessageType.Debug) {
                Console.WriteLine(lm.Message);
            }

            lock (_logMessagesForDB) {
                _logMessagesForDB.Add(lm);
            }
            if (Window != null && Window.Dispatcher != null) {
                Window.Dispatcher.Invoke(ThreadsafeLogging);
            }

            void ThreadsafeLogging()
            {
                if (lm.MessageType < MessageType.Debug) {
                    LogMessagesForWindow.Insert(0, lm.Message);
                }
                while (LogMessagesForWindow.Count > 100) {
                    LogMessagesForWindow.RemoveAt(99);
                }
            }
        }


        public void SaveToDatabase([NotNull] ScenarioSliceParameters slice)
        {
            lock (_logMessagesForDB) {
                var stages = _logMessagesForDB.Select(x => x.DstStage).Distinct().ToList();
                foreach (var stage in stages) {
                    var onestage = _logMessagesForDB.Where(x => x.DstStage == stage).ToList();
                    var msc = new MySqlConnection();
                    MySqlConnection.CreateTableIfNotExists<LogMessage>(stage, slice);
                    var db = msc.GetDatabaseConnection(stage, slice).Database;
                    db.BeginTransaction();
                    foreach (var logMessage in onestage) {
                        db.Save(logMessage);
                        _logMessagesForDB.Remove(logMessage);
                    }

                    db.CompleteTransaction();
                }
            }
        }
    }
}