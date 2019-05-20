using System;
using SharpWorker.DataStore;
using SharpWorker.Health.Models;

namespace SharpWorker.Health
{
    public sealed class HealthRecord : DataRecord
    {
        public HealthRecord()
        {
        }

        internal HealthRecord(
            DataTopic topic,
            ProcessHealth process,
            MemoryHealth memory,
            DriveHealth mainDrive,
            long databaseSize,
            bool isBackingUp,
            DriveHealth[] drives) : base(DateTime.UtcNow, topic)
        {
            Process = process;
            Memory = memory;
            MainDrive = mainDrive;
            DatabaseSize = databaseSize;
            IsBackingUp = isBackingUp;
            Drives = drives;
            Id = CreatedTimestamp.ToString();
        }

        public long DatabaseSize { get; set; }

        public DriveHealth[] Drives { get; set; }

        /// <inheritdoc />
        public override string Id { get; set; }

        public bool IsBackingUp { get; set; }
        public DriveHealth MainDrive { get; set; }
        public MemoryHealth Memory { get; set; }
        public ProcessHealth Process { get; set; }
    }
}