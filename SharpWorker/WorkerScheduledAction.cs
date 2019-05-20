using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWorker.Log;

namespace SharpWorker
{
    public class WorkerScheduledAction : IDisposable, INotifyPropertyChanged
    {
        protected static Random Random = new Random();

        private bool _isRunning;
        private DateTime? _lastRun;
        private DateTime? _nextRun;
        private WorkerScheduledActionStatus _status = WorkerScheduledActionStatus.Stopped;

        protected CancellationTokenSource CancellationTokenSource;
        protected Timer Timer;
        protected readonly Func<CancellationToken, Task> Action;

        // ReSharper disable once TooManyDependencies
        public WorkerScheduledAction(
            IWorker worker,
            string name,
            TimeSpan interval,
            Func<CancellationToken, Task> action,
            TimeSpan? startDelay) : this(worker, name, interval, action)
        {
            if (startDelay != null)
            {
                StartDelay = startDelay.Value;
            }
        }

        // ReSharper disable once TooManyDependencies
        public WorkerScheduledAction(IWorker worker, string name, TimeSpan interval, Func<CancellationToken, Task> action)
        {
            Worker = worker;
            Name = name;
            Interval = interval;
            Action = action;
            StartDelay = TimeSpan.FromSeconds(Random.Next(5, 60));
        }

        // ReSharper disable once TooManyDependencies
        public WorkerScheduledAction(
            IWorker worker,
            string name,
            TimeSpan interval,
            Action<CancellationToken> action,
            TimeSpan? startDelay) : this(
            worker, name, interval, action)
        {
            if (startDelay != null)
            {
                StartDelay = startDelay.Value;
            }
        }

        // ReSharper disable once TooManyDependencies
        public WorkerScheduledAction(
            IWorker worker,
            string name,
            TimeSpan interval,
            Action<CancellationToken> action) : this(
            worker,
            name,
            interval,
            token =>
            {
                // ReSharper disable once EventExceptionNotDocumented
                action.Invoke(token);

                return Task.CompletedTask;
            })
        {
        }

        public int Executions { get; protected set; }
        public TimeSpan Interval { get; }

        public DateTime? LastRun
        {
            get => _lastRun;
            private set
            {
                _lastRun = value;
                OnPropertyChanged(nameof(LastRun));
            }
        }

        public DateTime? NextRun
        {
            get => _nextRun;
            private set
            {
                _nextRun = value;
                OnPropertyChanged(nameof(NextRun));
            }
        }

        protected TimeSpan StartDelay { get; }

        public WorkerScheduledActionStatus Status
        {
            get => _status;
            protected set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        [JsonIgnore]
        public IWorker Worker { get; }

        public string Name { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Stop().Wait();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected internal virtual void Start()
        {
            lock (this)
            {
                if (Status != WorkerScheduledActionStatus.Stopped || Timer != null)
                {
                    return;
                }

                CancellationTokenSource = new CancellationTokenSource();
                NextRun = DateTime.UtcNow + StartDelay + Interval;
                Executions = 0;
                Timer = new Timer(OnTimerCallback, null, StartDelay, Interval);
                Status = WorkerScheduledActionStatus.Starting;
            }
        }

        protected internal virtual async Task Stop()
        {
            lock (this)
            {
                if (Timer == null)
                {
                    return;
                }

                CancellationTokenSource.Cancel();
                Timer?.Dispose();
                Timer = null;
                Status = WorkerScheduledActionStatus.Stopping;
            }

            while (true)
            {
                lock (this)
                {
                    if (!_isRunning)
                    {
                        break;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
            }

            lock (this)
            {
                Status = WorkerScheduledActionStatus.Stopped;
                NextRun = null;
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

        protected virtual async void OnTimerCallback(object state)
        {
            lock (this)
            {
                NextRun = DateTime.UtcNow + Interval;

                if (Status == WorkerScheduledActionStatus.Running ||
                    Status == WorkerScheduledActionStatus.Stopping ||
                    Status == WorkerScheduledActionStatus.Stopped ||
                    Timer == null)
                {
                    return;
                }

                Status = WorkerScheduledActionStatus.Running;
                _isRunning = true;
                LastRun = DateTime.UtcNow;
                Executions++;
            }

            Worker?.Logger?.Log(Worker, nameof(OnTimerCallback), LogType.Debug, "Running scheduled action {0}.", Name);

            try
            {
                await Action.Invoke(CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Worker?.Logger?.Log(Worker, Name, LogType.Error, e);
            }

            lock (this)
            {
                if (Status == WorkerScheduledActionStatus.Running)
                {
                    Status = WorkerScheduledActionStatus.Sleeping;
                }

                _isRunning = false;
            }
        }
    }
}