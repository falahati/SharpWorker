using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWorker.Log;

namespace SharpWorker
{
    public class CoordinatedWorker : IDisposable, INotifyPropertyChanged
    {
        public CoordinatedWorker(WorkerConfiguration workerConfiguration, IWorker worker)
        {
            Configuration = workerConfiguration;

            Worker = worker;

            Configuration.Alias = Configuration.Alias ?? Worker?.Name;

            ScheduledActions = Worker?.GetScheduledActions() ?? new WorkerScheduledAction[0];
            
            if (Worker != null)
            {
                Worker.PropertyChanged += WorkerOnPropertyChanged;
                Worker.Terminated += WorkerOnTerminated;
                if (IsCustomizable)
                {
                    Configuration.Options = (Worker as ICustomizableWorker)?.Options ?? Configuration.Options;
                }
            }
        }

        private async void WorkerOnTerminated(object sender, EventArgs e)
        {
            try
            {
                await Stop().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }

        public WorkerConfiguration Configuration { get; }


        public string Id { get; } = Guid.NewGuid().ToString("N");

        public bool IsCustomizable
        {
            get => !IsMissing && Worker is ICustomizableWorker;
        }

        public bool DoesProvideWebApi
        {
            get => !IsMissing && Worker is IWebApiWorker;
        }

        public bool IsMissing
        {
            get => Worker == null;
        }

        public WorkerScheduledAction[] ScheduledActions { get; private set; }

        public CoordinatedWorkerStatus Status { get; private set; }

        [JsonIgnore]
        public IWorker Worker { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (this)
            {
                Stop().Wait();

                if (ScheduledActions != null)
                {
                    foreach (var scheduledAction in ScheduledActions)
                    {
                        scheduledAction.Dispose();
                    }

                    ScheduledActions = null;
                }

                if (Worker != null)
                {
                    Worker.PropertyChanged -= WorkerOnPropertyChanged;
                    Worker.Terminated -= WorkerOnTerminated;
                }

                Worker?.Dispose();
                Worker = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // ReSharper disable once FlagArgument
        protected internal virtual async Task Start(bool delayed)
        {
            lock (this)
            {
                if (IsMissing)
                {
                    return;
                }

                if (Status != CoordinatedWorkerStatus.Stopped)
                {
                    return;
                }

                Status = CoordinatedWorkerStatus.Starting;
            }
            
            if (delayed)
            {
                Worker.Logger?.Log(Worker, nameof(Start), LogType.Debug,
                    "Coordinated worker {0} start delayed by {1} seconds.", Id, Configuration.StartDelay);
                await Task.Delay(TimeSpan.FromSeconds(Configuration.StartDelay)).ConfigureAwait(false);
            }

            Worker.Logger?.Log(Worker, nameof(Start), LogType.Debug, "Starting coordinated worker {0}.", Id);
            try
            {
                await Worker.Start().ConfigureAwait(false);

                foreach (var scheduledAction in ScheduledActions)
                {
                    scheduledAction.Start();
                }

                lock (this)
                {
                    Status = CoordinatedWorkerStatus.Running;
                }

                Worker.Logger?.Log(Worker, nameof(Start), LogType.Info, "Coordinated worker {0} started.", Id);
            }
            catch
            {
                Status = CoordinatedWorkerStatus.Stopped;

                throw;
            }
        }

        protected internal virtual async Task Stop()
        {
            lock (this)
            {
                if (Status != CoordinatedWorkerStatus.Running)
                {
                    return;
                }

                Status = CoordinatedWorkerStatus.Stopped;
            }

            Worker.Logger?.Log(Worker, nameof(Start), LogType.Debug, "Stopping coordinated worker {0}.", Id);

            foreach (var scheduledAction in ScheduledActions)
            {
                await scheduledAction.Stop().ConfigureAwait(false);
            }

            await Worker.Stop().ConfigureAwait(false);

            lock (this)
            {
                Status = CoordinatedWorkerStatus.Stopped;
            }

            Worker.Logger?.Log(Worker, nameof(Start), LogType.Info, "Coordinated worker {0} stopped.", Id);
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

        private void WorkerOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnPropertyChanged(nameof(Worker));
        }
    }
}