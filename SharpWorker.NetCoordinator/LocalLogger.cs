using System;
using System.Linq;
using ConsoleUtilities;
using SharpWorker.DataStore;
using SharpWorker.Log;

namespace SharpWorker.NetCoordinator
{
    internal class LocalLogger : Logger
    {
        private readonly ConsoleWriter _writer;

        /// <inheritdoc />
        public LocalLogger(DataStoreBase dataStore, ConsoleWriter writer) : base(dataStore)
        {
            _writer = writer;
        }

        /// <inheritdoc />
        // ReSharper disable once TooManyDependencies
        public LocalLogger(
            DataStoreBase dataStore,
            ConsoleWriter writer,
            string fileName,
            LogType fileLogLevel = LogType.Warning) : base(dataStore, fileName, fileLogLevel)
        {
            _writer = writer;
        }

        public bool Debug { get; set; } = false;
        public string Filtered { get; private set; }

        public void FilterByWorkerName(string workerName)
        {
            Filtered = workerName;
            Console.Clear();
            foreach (var logRecord in GetWorkerHistory(workerName).Reverse().ToArray())
            {
                WriteRecord(logRecord);
            }
        }

        public void FilterBySystem()
        {
            Filtered = "~";
            Console.Clear();
            foreach (var logRecord in GetWorkerHistory(null).Reverse().ToArray())
            {
                WriteRecord(logRecord);
            }
        }

        public void FilterClear()
        {
            Filtered = null;
            Console.Clear();
            foreach (var logRecord in GlobalHistory.ToArray().Reverse().ToArray())
            {
                WriteRecord(logRecord);
            }
        }

        /// <inheritdoc />
        // ReSharper disable once MethodNameNotMeaningful
        protected override void Log(DataTopic localDataTopic, LogRecord logRecord)
        {
            base.Log(localDataTopic, logRecord);

            if (string.IsNullOrWhiteSpace(Filtered))
            {
                WriteRecord(logRecord);
            }
            else if (Filtered.Equals(localDataTopic.Subject, StringComparison.InvariantCultureIgnoreCase) ||
                     (Filtered == "~" && localDataTopic.Subject == null))
            {
                WriteRecord(logRecord);
            }
        }

        // ReSharper disable once FlagArgument
        protected void WriteRecord(LogRecord logRecord)
        {
            switch (logRecord.Type)
            {
                case LogType.Warning:
                    _writer?.PrintWarning(logRecord.ToString());

                    break;
                case LogType.Error:
                case LogType.Fatal:
                    _writer?.PrintError(logRecord.ToString());

                    break;
                case LogType.Info:
                    _writer?.PrintSuccess(logRecord.ToString());

                    break;
                default:

                    if (Debug)
                    {
                        _writer?.PrintMessage(logRecord.ToString());
                    }

                    break;
            }
        }
    }
}