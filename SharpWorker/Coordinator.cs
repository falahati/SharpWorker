using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SharpWorker.DataStore;
using SharpWorker.DataStore.WebApiControllers;
using SharpWorker.Log;
using SharpWorker.WebApi;

namespace SharpWorker
{
    public class Coordinator : IDisposable, INotifyPropertyChanged
    {
        private static IWorkerResolver _workerResolver = new DefaultWorkerResolver();
        public IWebApiWorkerService WebApiService { get; }

        private List<CoordinatedWorker> _workers = new List<CoordinatedWorker>();

        public Coordinator(
            CoordinatorSettings settings,
            Logger logger,
            DataStoreBase dataStore,
            IWebApiWorkerService webApiService = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Settings = settings;
            Logger = logger;
            DataStore = dataStore;
            WebApiService = webApiService;

            if (Settings.IsLoggerWebApiEnable)
            {
                WebApiService?.RegisterController(new LoggerController(Logger));
            }

            if (Settings.IsCoordinatorWebApiEnable)
            {
                WebApiService?.RegisterController(new CoordinatorController(this, Logger));
            }

            if (Settings.IsDataStoreWebApiEnable)
            {
                WebApiService?.RegisterController(new DataRecordsController(DataStore, Logger));
                WebApiService?.RegisterController(new DataProvidersController(DataStore, Logger));
                WebApiService?.RegisterController(new DataRecordAttributesController(DataStore, Logger));
            }

            _workers.AddRange(Settings.Workers.Select(CreateCoordinatedWorker));
            Logger.PropertyChanged += LoggerOnPropertyChanged;
        }

        public DataStoreBase DataStore { get; }
        public Logger Logger { get; }

        public static IWorkerResolver WorkerResolver
        {
            get => _workerResolver;
            set => _workerResolver = value ?? new DefaultWorkerResolver();
        }

        public CoordinatorSettings Settings { get; }

        public CoordinatedWorker[] Workers
        {
            get
            {
                lock (_workers)
                {
                    return _workers.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            StopAll().Wait();

            lock (this)
            {
                lock (_workers)
                {
                    foreach (var worker in _workers)
                    {
                        worker.Dispose();
                    }
                }

                _workers = null;
            }

            WebApiService?.UnRegisterController<LoggerController>();
            WebApiService?.UnRegisterController<CoordinatorController>();
            WebApiService?.UnRegisterController<DataRecordsController>();
            WebApiService?.UnRegisterController<DataProvidersController>();
            //_webApiService?.UnRegisterController<DataRecordAttributesController>();
        }


        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual string AddWorker(WorkerConfiguration configuration)
        {
            lock (_workers)
            {
                var newCoordinator = CreateCoordinatedWorker(configuration);

                if (newCoordinator == null)
                {
                    return null;
                }

                _workers.Add(newCoordinator);

                Settings.Workers = Settings.Workers.Concat(new[] {configuration}).ToArray();

                return newCoordinator.Id;
            }
        }
        public virtual bool DeleteWorker(string workerId)
        {
            lock (_workers)
            {
                var worker = _workers.FirstOrDefault(coordinatedWorker =>
                    coordinatedWorker.Id.Equals(workerId, StringComparison.InvariantCultureIgnoreCase));

                if (worker == null ||
                    worker.Status != CoordinatedWorkerStatus.Stopped)
                {
                    return false;
                }

                if (_workers.Remove(worker))
                {

                    DestroyCoordinatedWorker(worker);

                    Settings.Workers = Settings.Workers.Except(new[] {worker.Configuration}).ToArray();

                    return true;
                }

                return false;
            }
        }

        public virtual string ChangeWorkerConfiguration(string workerId, WorkerConfiguration configuration)
        {
            lock (_workers)
            {
                var worker = _workers.FirstOrDefault(coordinatedWorker =>
                    coordinatedWorker.Id.Equals(workerId, StringComparison.InvariantCultureIgnoreCase));

                if (worker == null ||
                    worker.Status != CoordinatedWorkerStatus.Stopped ||
                    worker.Configuration.WorkerType != configuration.WorkerType)
                {
                    return null;
                }

                var newCoordinator = CreateCoordinatedWorker(configuration);

                if (newCoordinator == null)
                {
                    return null;
                }

                if (_workers.Remove(worker))
                {

                    DestroyCoordinatedWorker(worker);

                    _workers.Add(newCoordinator);

                    return newCoordinator.Id;
                }

                return null;
            }
        }

        public virtual void FlushSettings()
        {
            Settings.Save();
            OnPropertyChanged(nameof(Settings));
        }

        public virtual async Task RestartWorker(string workerId)
        {
            await StopWorker(workerId).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await StartWorker(workerId, false).ConfigureAwait(false);
        }

        public Task StartAll(bool delayed)
        {
            var tasks = new List<Task>();

            lock (this)
            {
                lock (_workers)
                {
                    foreach (var worker in _workers)
                    {
                        if (worker.Configuration.AutoStart)
                        {
                            tasks.Add(worker.Start(delayed));
                        }
                    }
                }
            }
            
            return Task.WhenAll(tasks.ToArray());
        }

        public virtual Task StartWorker(string workerId, bool delayed)
        {
            lock (_workers)
            {
                var worker = _workers.FirstOrDefault(coordinatedWorker =>
                    coordinatedWorker.Id.Equals(workerId, StringComparison.InvariantCultureIgnoreCase));

                if (worker == null)
                {
                    return Task.CompletedTask;
                }

                return worker.Start(delayed);
            }
        }

        public Task StopAll()
        {
            var tasks = new List<Task>();

            lock (this)
            {
                lock (_workers)
                {
                    foreach (var worker in _workers)
                    {
                        tasks.Add(worker.Stop());
                    }
                }
            }

            return Task.WhenAll(tasks.ToArray());
        }

        public virtual Task StopWorker(string workerId)
        {
            lock (_workers)
            {
                var worker = _workers.FirstOrDefault(coordinatedWorker =>
                    coordinatedWorker.Id.Equals(workerId, StringComparison.InvariantCultureIgnoreCase));

                if (worker == null)
                {
                    return Task.CompletedTask;
                }

                return worker.Stop();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch
            {
                // ignored
            }
        }

        public virtual CoordinatedWorker CreateCoordinatedWorker(WorkerConfiguration configuration)
        {
            var worker = WorkerResolver?.ActivateWorker(this, DataStore, Logger, configuration);

            var coordinatedWorker = new CoordinatedWorker(configuration, worker);
            
            if (coordinatedWorker.DoesProvideWebApi && worker is IWebApiWorker webApiWorker)
            {
                WebApiService.RegisterWebApiWorker(webApiWorker);
            }

            coordinatedWorker.PropertyChanged += WorkerOnPropertyChanged;

            return coordinatedWorker;
        }

        public virtual void DestroyCoordinatedWorker(CoordinatedWorker worker)
        {
            worker.PropertyChanged -= WorkerOnPropertyChanged;

            if (worker.DoesProvideWebApi && worker.Worker is IWebApiWorker webApiWorker)
            {
                WebApiService.UnRegisterWebApiWorker(webApiWorker);
            }

            worker.Dispose();
        }

        private void LoggerOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnPropertyChanged(nameof(Logger));
        }

        private void WorkerOnPropertyChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnPropertyChanged(nameof(Workers));
        }
    }
}