using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpWorker.DataStore;
using SharpWorker.Health.Models;
using SharpWorker.Health.Native;
using SharpWorker.Log;
using SharpWorker.WebApi;

namespace SharpWorker.Health
{
    public class HealthWorker : IWebApiWorker
    {
        private readonly DriveInfo _currentDrive;
        private readonly DataStoreBase _dataStore;
        private readonly SystemMemoryInfo _memoryInfo;
        private readonly Process _thisProcess;
        private readonly DataTopic _topic;


        private HealthController _controller;
        private HealthRecord _currentHealthReport;
        private DateTime _lastCallback;
        private TimeSpan _lastProcessorTime;

        public HealthWorker(DataStoreBase dataStore, Logger logger)
        {
            Logger = logger;

            _dataStore = dataStore;
            _topic = dataStore.GetOrCreateTopic(typeof(HealthRecord), null);
            _memoryInfo = new SystemMemoryInfo();
            _thisProcess = Process.GetCurrentProcess();
            _lastProcessorTime = _thisProcess.TotalProcessorTime;
            _lastCallback = DateTime.Now;

            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            _currentDrive = DriveInfo.GetDrives()
                .Where(info => currentDirectory.FullName.StartsWith(info.RootDirectory.FullName))
                .OrderByDescending(info => info.RootDirectory.FullName.Length).FirstOrDefault();
        }

        public HealthRecord CurrentHealthReport
        {
            get => _currentHealthReport;
            private set
            {
                _currentHealthReport = value;

                OnPropertyChanged(nameof(CurrentHealthReport));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Stop().Wait();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public WebApiController[] GetWebApiControllers()
        {
            lock (this)
            {
                if (_controller == null)
                {
                    _controller = new HealthController(this, Logger);
                }
            }

            return new WebApiController[] { _controller };
        }

        /// <inheritdoc />
        public event EventHandler Terminated;

        /// <inheritdoc />
        public long DataPoints { get; protected set; }

        /// <inheritdoc />
        public DataTopic[] DataTopics
        {
            get => new[] {_topic};
        }

        /// <inheritdoc />
        public WorkerScheduledAction[] GetScheduledActions()
        {
            return new[]
            {
                new WorkerScheduledAction(this, nameof(UpdateHealthCallback), TimeSpan.FromSeconds(10), (Action<CancellationToken>) UpdateHealthCallback)
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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateHealthCallback(CancellationToken cancellationToken)
        {
            try
            {
                lock (this)
                {
                    var averageProcessorUsage = _thisProcess.TotalProcessorTime.Ticks /
                                                (double) (DateTime.Now - _thisProcess.StartTime).Ticks *
                                                100;
                    var processorUsage = (_thisProcess.TotalProcessorTime - _lastProcessorTime).Ticks /
                                         (double) (DateTime.Now - _lastCallback).Ticks *
                                         100;
                    _lastCallback = DateTime.Now;
                    _lastProcessorTime = _thisProcess.TotalProcessorTime;

                    ProcessHealth processHealth = null;
                    MemoryHealth memoryHealth = null;
                    DriveHealth driveHealth = null;
                    DriveHealth[] driveHealths = new DriveHealth[0];

                    try
                    {
                        processHealth = new ProcessHealth(_thisProcess, processorUsage, averageProcessorUsage);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(this, _topic, LogType.Warning, e);
                    }

                    try
                    {
                        memoryHealth = new MemoryHealth(_memoryInfo);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(this, _topic, LogType.Warning, e);
                    }

                    try
                    {
                        driveHealth = new DriveHealth(_currentDrive);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(this, _topic, LogType.Warning, e);
                    }

                    try
                    {
                        driveHealths = DriveInfo.GetDrives().Where(drive => drive.IsReady).Select(drive =>
                        {
                            try
                            {
                                return new DriveHealth(drive);
                            }
                            catch (Exception e)
                            {
                                Logger.Log(this, _topic, LogType.Warning, e);

                                return null;
                            }
                        }).Where(drive => drive != null).ToArray();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(this, _topic, LogType.Warning, e);
                    }

                    var currentHealth = new HealthRecord(
                        _topic,
                        processHealth,
                        memoryHealth,
                        driveHealth,
                        _dataStore.GetDatabaseSize(),
                        _dataStore.IsBackingUp,
                        driveHealths);

                    CurrentHealthReport = currentHealth;

                    lock (_dataStore)
                    {
                        _dataStore.Upsert(_topic, new[] {currentHealth});
                    }

                    DataPoints++;
                }
            }
            catch (Exception e)
            {
                Logger.Log(this, _topic, LogType.Error, e);
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
                // ignored
            }
        }
    }
}