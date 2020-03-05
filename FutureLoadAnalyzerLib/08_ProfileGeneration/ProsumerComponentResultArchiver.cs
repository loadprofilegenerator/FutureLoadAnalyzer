using System;
using System.Collections.Concurrent;
using System.Threading;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    public class ProsumerComponentResultArchiver : IDisposable {
        private readonly Stage _myStage;
        [NotNull] private readonly ServiceRepository _services;
        [CanBeNull] private readonly SaveableEntry<Prosumer> _generationSa;
        [CanBeNull] private readonly SaveableEntry<Prosumer> _loadSa;
        [NotNull] private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        // ReSharper disable once NotNullMemberIsNotInitialized
        public ProsumerComponentResultArchiver( Stage myStage, [NotNull] ScenarioSliceParameters parameters,
                                                HouseProcessor.ProcessingMode processingMode,
                                                [NotNull] ServiceRepository services)
        {
            _myStage = myStage;
            _services = services;
            if (processingMode == HouseProcessor.ProcessingMode.Collecting) {
                _generationSa = SaveableEntry<Prosumer>.GetSaveableEntry(_services.SqlConnectionPreparer.GetDatabaseConnection(myStage, parameters, DatabaseCode.HouseProfiles),
                    SaveableEntryTableType.HouseGeneration,
                    _services.Logger);
                _generationSa.MakeCleanTableForListOfFields(false);
                _loadSa = SaveableEntry<Prosumer>.GetSaveableEntry(_services.SqlConnectionPreparer.GetDatabaseConnection(myStage, parameters, DatabaseCode.HouseProfiles),
                    SaveableEntryTableType.HouseLoad,
                    _services.Logger);
                _loadSa.MakeCleanTableForListOfFields(false);
            }
            _myThread = ThreadProvider.Get().MakeThreadAndStart(SafeRun, "Archiver");
        }
        [NotNull][ItemNotNull] private readonly BlockingCollection<Prosumer> _myqueue = new BlockingCollection<Prosumer>(2000);
        [NotNull] private readonly Thread _myThread;

        public void Archive([NotNull] Prosumer prosumer)
        {
            if (_myqueue.Count >2000) {
                Console.WriteLine("Archiver queue at over 2000!");
            }
            _myqueue.Add(prosumer);
        }
        private void SafeRun()
        {
            try {
                _services.Logger.Info("Starting Prosumer Results Archiving Thread", _myStage,nameof(ProsumerComponentResultArchiver));
                //main thread
                while (!_cancellationTokenSource.Token.IsCancellationRequested) {
                    try {
                        var prosumer = _myqueue.Take(_cancellationTokenSource.Token);
                        ArchiveOneEntry(prosumer);
                    }

                    catch (OperationCanceledException) {
                        _services.Logger.Info("canceled waiting",Stage.ProfileGeneration,"Archiver");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                        _services.Logger.ErrorM(ex.GetType().FullName + ": " +
                            ex.Message + " " + ex.StackTrace, _myStage, "ComponentArchiver");
                    }
                }
                _services.Logger.Info("Shutting down Prosumer Results Archiving Thread, " + _myqueue.Count + " entries left in queue", _myStage, nameof(ProsumerComponentResultArchiver));
                //clean up queue
                while (_myqueue.Count > 0) {
                    var prosumer = _myqueue.Take();
                    ArchiveOneEntry(prosumer);
                }

                if (_generationSa != null) {
                    _generationSa.SaveDictionaryToDatabase(_services.Logger);
                }

                if (_loadSa != null) {
                    _loadSa.SaveDictionaryToDatabase(_services.Logger);
                }

                _services.Logger.Info("Finished Shutting down Prosumer Results Archiving Thread, archived " + _archiveCount  + " prosumers", _myStage, nameof(ProsumerComponentResultArchiver));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
                _services.Logger.ErrorM(ex.Message + " " + ex.StackTrace,_myStage,"ComponentArchiver");
            }
        }

        private int _archiveCount;

        private void ArchiveOneEntry([NotNull] Prosumer prosumer)
        {
            _archiveCount++;
            if (prosumer.GenerationOrLoad == GenerationOrLoad.Load) {
                if (_loadSa == null) {
                    throw new FlaException("load was null");
                }
                _loadSa.AddRow(prosumer);
                if (_loadSa.RowEntries.Count > 50) {
                    _loadSa.SaveDictionaryToDatabase(_services.Logger);
                }
            }

            if (prosumer.GenerationOrLoad == GenerationOrLoad.Generation) {
                if (_generationSa == null) {
                    throw new FlaException("load was null");
                }
                _generationSa.AddRow(prosumer);
                if (_generationSa.RowEntries.Count > 50) {
                    _generationSa.SaveDictionaryToDatabase(_services.Logger);
                }
            }
        }

        public void FinishSavingEverything()
        {
            _cancellationTokenSource.Cancel();
            _myThread.Join();
        }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            if (!_cancellationTokenSource.IsCancellationRequested) {
                _cancellationTokenSource.Cancel();
                _myThread.Join();
            }
            _cancellationTokenSource.Dispose();
            _myqueue.Dispose();
        }
    }
}