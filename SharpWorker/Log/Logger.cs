using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SharpWorker.DataStore;
using SharpWorker.DataStore.Query;

namespace SharpWorker.Log
{
    // ReSharper disable once ClassTooBig
    public class Logger : INotifyPropertyChanged
    {
        private readonly LogHistory _history = new LogHistory(100);
        private readonly Dictionary<string, LogHistory> _workerHistory = new Dictionary<string, LogHistory>();

        private readonly Dictionary<string, DataTopic> _workerTopics = new Dictionary<string, DataTopic>();
        protected readonly DataStoreBase DataStore;

        protected readonly LogType FileLogLevel;

        public Logger(DataStoreBase dataStore) : this(dataStore, null)
        {
        }

        public Logger(DataStoreBase dataStore, string fileName, LogType fileLogLevel = LogType.Warning)
        {
            DataStore = dataStore;
            Filename = fileName;
            FileLogLevel = fileLogLevel;

            if (!string.IsNullOrWhiteSpace(Filename))
            {
                Filename = Path.GetFullPath(Filename);
                var directory = Path.GetDirectoryName(Filename);

                if (string.IsNullOrWhiteSpace(directory))
                {
                    Filename = null;
                }
                else
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Create(Filename).Dispose();
                }
            }
        }

        public string Filename { get; protected set; }

        public virtual LogRecord[] GlobalHistory
        {
            get
            {
                lock (_history)
                {
                    return _history.Reverse().ToArray();
                }
            }
        }

        public virtual int HistoryCapacity
        {
            get
            {
                lock (_history)
                {
                    return _history.MaxCapacity;
                }
            }
            set
            {
                lock (_history)
                {
                    _history.MaxCapacity = value;
                }

                lock (_workerHistory)
                {
                    foreach (var pair in _workerHistory)
                    {
                        pair.Value.MaxCapacity = value;
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public LogRecord[] GetWorkerHistory(string worker)
        {
            lock (_workerHistory)
            {
                if (_workerHistory.ContainsKey(worker))
                {
                    return _workerHistory[worker]?.Reverse().ToArray() ?? new LogRecord[0];
                }
            }

            return new LogRecord[0];
        }

        public LogRecord[] GetWorkerRecords(string worker)
        {
            if (DataStore == null)
            {
                return new LogRecord[0];
            }

            lock (DataStore)
            {
                var topic = DataStore.GetTopic(typeof(LogRecord), worker, new string[0]);

                if (topic == null)
                {
                    return new LogRecord[0];
                }

                return DataStore.GetRecords<LogRecord>(topic, null,
                    new DataStoreSortQuery(new DataStoreQueryField(nameof(LogRecord.CreatedTimestamp)),
                        DataStoreSortQueryDirection.Descending)).ToArray();
            }
        }

        public string[] GetHistoryWorkers()
        {
            lock (_workerHistory)
            {
                return _workerHistory?.Keys.Distinct().ToArray();
            }
        }

        public string[] GetWorkers()
        {
            if (DataStore != null)
            {
                lock (DataStore)
                {
                    var result = DataStore?.GetTopics(typeof(LogRecord))?.Select(topic => topic.Subject).Distinct().ToArray();

                    if (result != null && result.Length > 0)
                    {
                        return result;
                    }
                }
            }

            lock (_workerHistory)
            {
                var result = _workerHistory?.Keys.Distinct().ToArray();

                if (result.Length > 0)
                {
                    return result;
                }
            }

            lock (_workerTopics)
            {
                var result = _workerTopics?.Keys.Distinct().ToArray();

                if (result.Length > 0)
                {
                    return result;
                }
            }

            return new string[0];
        }

        // ReSharper disable once FlagArgument
        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public virtual void Log(string worker, string topic, LogType logType, string message, params object[] args)
        {
            worker = worker ?? "{System}";
            topic = topic ?? "{General}";
            message = args?.Length > 0 ? string.Format(message, args) : message;

            DataTopic localDataTopic;

            lock (_workerTopics)
            {
                if (!_workerTopics.ContainsKey(worker))
                {
                    _workerTopics.Add(worker,
                        DataStore?.GetOrCreateTopic(typeof(LogRecord), worker) ??
                        new DataTopic(typeof(LogRecord), worker));
                }

                localDataTopic = _workerTopics[worker];
            }

            Log(localDataTopic, new LogRecord(localDataTopic, topic, logType, message));
        }


        // ReSharper disable once MethodNameNotMeaningful
        public void Log(LogType logType, string message, params object[] args)
        {
            Log(null, logType, message, args);
        }


        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(IWorker worker, DataTopic topic, LogType logType, string message, params object[] args)
        {
            Log(worker, topic?.ToString(), logType, message, args);
        }

        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(IWorker worker, DataTopic topic, LogType logType, Exception exception)
        {
            Log(worker?.Name, topic, logType, exception);
        }

        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(IWorker worker, string topic, LogType logType, string message, params object[] args)
        {
            Log(worker?.Name, topic, logType, message, args);
        }

        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(IWorker worker, string topic, LogType logType, Exception exception)
        {
            Log(worker?.Name, topic, logType, exception);
        }


        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(string worker, DataTopic topic, LogType logType, string message, params object[] args)
        {
            Log(worker, topic?.ToString(), logType, message, args);
        }

        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(string worker, DataTopic topic, LogType logType, Exception exception)
        {
            Log(worker, topic?.ToString(), logType, exception);
        }

        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(string worker, string topic, LogType logType, Exception exception)
        {
            Log(worker, topic, logType,
                exception.Message + Environment.NewLine + exception.StackTrace);

            if (exception.InnerException != null)
            {
                Log(worker, topic, logType, exception.InnerException);
            }
        }


        // ReSharper disable once MethodNameNotMeaningful
        // ReSharper disable once TooManyArguments
        public void Log(string topic, LogType logType, string message, params object[] args)
        {
            Log((string) null, topic, logType, message, args);
        }

        // ReSharper disable once MethodNameNotMeaningful
        public void Log(string topic, LogType logType, Exception exception)
        {
            Log((string) null, topic, logType, exception);
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once MethodNameNotMeaningful
        protected virtual void Log(DataTopic localDataTopic, LogRecord logRecord)
        {
            if (DataStore != null)
            {
                lock (DataStore)
                {
                    DataStore.Upsert(localDataTopic, new[]
                    {
                        logRecord
                    });
                }
            }

            lock (_history)
            {
                _history.AddToHistory(logRecord);
            }

            lock (_workerHistory)
            {
                if (!_workerHistory.ContainsKey(localDataTopic.Subject))
                {
                    _workerHistory.Add(localDataTopic.Subject, new LogHistory(HistoryCapacity));
                }

                _workerHistory[localDataTopic.Subject].AddToHistory(logRecord);
            }

            if (!string.IsNullOrWhiteSpace(Filename) && logRecord.Type >= FileLogLevel)
            {
                try
                {
                    lock (this)
                    {
                        File.AppendAllLines(Filename, new[] {logRecord.ToString()});
                    }
                }
                catch
                {
                    // ignore
                }
            }

            OnPropertyChanged(nameof(GlobalHistory));
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
    }
}