using System;
using System.Diagnostics;

namespace SharpWorker.Health.Models
{
    public class ProcessHealth
    {
        public ProcessHealth(Process process, double processorUsage, double averageProcessorUsage)
        {
            MemoryUsageBytes = process.WorkingSet64;
            PeakMemoryUsageBytes = process.PeakWorkingSet64;
            Started = process.StartTime;
            ProcessorUsagePercentage = processorUsage;
            AverageProcessorUsagePercentage = averageProcessorUsage;
        }

        public ProcessHealth()
        {
        }

        public double AverageProcessorUsagePercentage { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long PeakMemoryUsageBytes { get; set; }
        public double ProcessorUsagePercentage { get; set; }
        public DateTime Started { get; set; }
    }
}