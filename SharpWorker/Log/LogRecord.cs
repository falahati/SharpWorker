using System;
using Newtonsoft.Json;
using SharpWorker.DataStore;
using SharpWorker.DataStore.Attributes;

namespace SharpWorker.Log
{
    [DataRecordFieldIndex(nameof(Type))]
    [DataRecordFieldIndex(nameof(Topic))]
    public sealed class LogRecord : DataRecord
    {
        public LogRecord(
            DataTopic dataTopic,
            string topic,
            LogType type,
            string message) : base(DateTime.UtcNow, dataTopic)
        {
            Topic = topic;
            Type = type;
            Message = message;
            Id = DateTime.UtcNow.ToString("O") + "-" + Guid.NewGuid().ToString("N");
        }

        public LogRecord()
        {
            
        }

        /// <inheritdoc />
        public override string Id { get; set; }

        public string Message { get; set; }
        public string Topic { get; set; }
        public LogType Type { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                "{0}: [{1}] ([{2}].[{3}]) {4}",
                CreatedDateTime?.ToString("s") ?? "--",
                Type.ToString(),
                DataTopicSubject,
                Topic,
                Message);
        }
    }
}