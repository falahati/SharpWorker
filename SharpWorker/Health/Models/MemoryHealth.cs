using System;
using SharpWorker.Health.Native;

namespace SharpWorker.Health.Models
{
    public class MemoryHealth
    {
        public MemoryHealth()
        {
        }

        internal MemoryHealth(SystemMemoryInfo systemMemoryInfo)
        {
                TotalBytes = (long)systemMemoryInfo.TotalPhysicalMemory;
                AvailableBytes = (long)systemMemoryInfo.AvailablePhysicalMemory;
        }

        public long AvailableBytes { get; set; }
        public long TotalBytes { get; set; }
    }
}