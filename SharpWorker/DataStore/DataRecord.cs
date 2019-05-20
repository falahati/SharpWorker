using System;
using SharpWorker.DataStore.Attributes;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataStore
{
    [WebApiTypeDiscriminator]
    [DataRecordFieldIndex(nameof(CreatedTimestamp))]
    [DataRecordFieldIndex(nameof(LastUpdateTimestamp))]
    [DataRecordFieldIndex(nameof(DataTopicId))]
    [DataRecordFieldIndex(nameof(DataTopicSubject))]
    public abstract class DataRecord
    {
        protected DataRecord(DataTopic topic)
        {
            DataTopicId = topic.Id;
            DataTopicSubject = topic.Subject;
        }

        protected DataRecord(DateTime dateTime, DataTopic dataTopic) : this(dataTopic)
        {
            CreatedTimestamp = dateTime.ToUniversalTime().Ticks;
        }

        protected DataRecord()
        {
        }

        [DataField(true)]
        public virtual DateTime? CreatedDateTime
        {
            get => CreatedTimestamp == null || CreatedTimestamp <= 0
                ? (DateTime?) null
                : new DateTime(CreatedTimestamp.Value, DateTimeKind.Utc);
            set => CreatedTimestamp = value?.ToUniversalTime().Ticks;
        }

        public long? CreatedTimestamp { get; set; }
        public string DataTopicId { get; set; }
        public string DataTopicSubject { get; set; }

        public abstract string Id { get; set; }

        [DataField(true)]
        public virtual DateTime? LastUpdateDateTime
        {
            get => LastUpdateTimestamp == null || LastUpdateTimestamp <= 0
                ? (DateTime?) null
                : new DateTime(LastUpdateTimestamp.Value, DateTimeKind.Utc);
            set => LastUpdateTimestamp = value?.ToUniversalTime().Ticks;
        }

        public long? LastUpdateTimestamp { get; set; }

        public virtual void OnSave(DataStoreBase dataStore, DataTopic topic)
        {
            LastUpdateDateTime = DateTime.UtcNow;
        }
    }
}