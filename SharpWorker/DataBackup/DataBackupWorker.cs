using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWorker.DataBackup.SharpZip;
using SharpWorker.DataStore;
using SharpWorker.Log;
using SharpWorker.WebApi;

namespace SharpWorker.DataBackup
{
    public class DataBackupWorker : ICustomizableWorker, IWebApiWorker
    {
        protected const string DuplicateBackupExtension = ".backup";
        protected const string FailedBackupExtension = ".failed";
        protected const string OngoingBackupExtension = ".tmp";
        private readonly DataStoreBase _dataStore;
        private IDataBackupArchiver _backupArchiver = new SharpZipArchiver();

        private DataBackupController _controller;
        private bool _isBackingUp;

        public DataBackupWorker(DataBackupWorkerOptions options, Logger logger, DataStoreBase dataStore)
        {
            _dataStore = dataStore;
            Options = options ?? new DataBackupWorkerOptions();
            Logger = logger;

            BackupDirectory = new DirectoryInfo(Path.GetFullPath(Options.Directory));

            if (!BackupDirectory.Exists)
            {
                BackupDirectory.Create();
            }
        }

        [JsonIgnore]
        public IDataBackupArchiver BackupArchiver
        {
            get => _backupArchiver;
            set => _backupArchiver = value ?? new SharpZipArchiver();
        }

        protected DirectoryInfo BackupDirectory { get; }

        public bool IsBackingUp
        {
            get => _isBackingUp || _dataStore?.IsBackingUp == true;
            set => _isBackingUp = value;
        }

        public DataBackupWorkerOptions Options { get; }

        /// <inheritdoc />
        WorkerOptions ICustomizableWorker.Options
        {
            get => Options;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public WebApiController[] GetWebApiControllers()
        {
            lock (this)
            {
                if (_controller == null)
                {
                    _controller = new DataBackupController(this, Logger);
                }
            }

            return new WebApiController[] {_controller};
        }

        /// <inheritdoc />
        public long DataPoints
        {
            get => 0;
        }

        /// <inheritdoc />
        public DataTopic[] DataTopics
        {
            get => new DataTopic[0];
        }

        /// <inheritdoc />
        public WorkerScheduledAction[] GetScheduledActions()
        {
            return new[]
            {
                new WorkerScheduledAction(
                    this,
                    nameof(BackupDataStoreCallback),
                    TimeSpan.FromHours(Options.HourlyInterval),
                    BackupDataStoreCallback,
                    Options.OnStart ? (TimeSpan?) null : TimeSpan.FromHours(Options.HourlyInterval))
            };
        }

        /// <inheritdoc />
        public Logger Logger { get; }

        /// <inheritdoc />
        public string Name
        {
            get => GetType().Name;
        }

        /// <inheritdoc />
        public Task Start()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Stop()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public event EventHandler Terminated;

        public async Task BackupDataStoreCallback(CancellationToken cancellationToken)
        {
            lock (this)
            {
                if (_dataStore == null || IsBackingUp)
                {
                    return;
                }

                IsBackingUp = true;
            }

            var backupArchiver = BackupArchiver;

            var backupFilename = Path.Combine(BackupDirectory.FullName,
                DateTime.UtcNow.ToString("s").Replace(":", "-") + backupArchiver.FileExtension);

            try
            {
                if (File.Exists(backupFilename))
                {
                    File.Move(backupFilename, backupFilename + DuplicateBackupExtension);
                }
            }
            catch
            {
                // ignored
            }

            // Create archive from ongoing file

            try
            {
                using (var archive = backupArchiver.CreateNewArchive(backupFilename + OngoingBackupExtension))
                {
                    await _dataStore.Archive(cancellationToken, archive).ConfigureAwait(false);
                }

                try
                {
                    if (cancellationToken.IsCancellationRequested) // If canceled
                    {
                        // Rename to failed
                        File.Move(backupFilename + OngoingBackupExtension, backupFilename + FailedBackupExtension);
                    }
                    else
                    {
                        // Rename to success
                        File.Move(backupFilename + OngoingBackupExtension, backupFilename);
                    }
                }
                catch
                {
                    // ignore
                }
            }
            catch
            {
                try
                {
                    // Rename to failed
                    File.Move(backupFilename + OngoingBackupExtension, backupFilename + FailedBackupExtension);
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                if (File.Exists(backupFilename)) // If success
                {
                    // Delete duplicate backup
                    if (File.Exists(backupFilename + DuplicateBackupExtension))
                    {
                        File.Delete(backupFilename + DuplicateBackupExtension);
                    }

                    // Cleanup backups
                    BackupCleanup(backupFilename);
                }
                else // If failed
                {
                    // Rename last duplicate backup
                    if (File.Exists(backupFilename + DuplicateBackupExtension))
                    {
                        File.Move(backupFilename + DuplicateBackupExtension, backupFilename);
                    }
                }
            }
            catch
            {
                // ignore
            }

            lock (this)
            {
                IsBackingUp = false;
            }
        }

        // ReSharper disable once ExcessiveIndentation
        public virtual DataBackupStatus[] GetBackups()
        {
            try
            {
                var backups = new List<DataBackupStatus>();

                foreach (var fileInfo in BackupDirectory.GetFiles("*"))
                {
                    var status = DataBackupState.Done;

                    if (fileInfo.Name.EndsWith(FailedBackupExtension))
                    {
                        status = DataBackupState.Failed;
                    }
                    else if (fileInfo.Name.EndsWith(OngoingBackupExtension))
                    {
                        status = DataBackupState.Ongoing;

                        if (fileInfo.LastWriteTimeUtc - DateTime.UtcNow > TimeSpan.FromHours(1) && !IsBackingUp)
                        {
                            status = DataBackupState.Abandoned;
                        }
                    }
                    else if (fileInfo.Name.Contains(DuplicateBackupExtension))
                    {
                        status = DataBackupState.Archived;
                    }

                    backups.Add(new DataBackupStatus(fileInfo, status));
                }

                return backups.ToArray();
            }
            catch
            {
                // ignored
            }

            return null;
        }

        protected virtual void BackupCleanup(string backupFilename)
        {
            if (Options.HourlyCleanup >= 0)
            {
                try
                {
                    var backupFullPath = Path.GetFullPath(backupFilename);

                    foreach (var fileInfo in BackupDirectory.GetFiles("*"))
                    {
                        if (!fileInfo.FullName.Equals(backupFullPath) &&
                            (DateTime.Now - fileInfo.CreationTime).TotalHours >
                            Options.HourlyCleanup)
                        {
                            try
                            {
                                fileInfo.Delete();
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            try
            {
                PropertyChanged?.Invoke(this, e);
            }
            catch
            {
                // ignore
            }
        }

        protected virtual void OnTerminated()
        {
            try
            {
                Terminated?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
            }
        }
    }
}