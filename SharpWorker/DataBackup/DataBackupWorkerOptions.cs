namespace SharpWorker.DataBackup
{
    public class DataBackupWorkerOptions : WorkerOptions
    {
        public string Directory { get; set; } = "Backups";
        public int HourlyCleanup { get; set; } = -1;
        public int HourlyInterval { get; set; } = 24;
        public bool OnStart { get; set; } = false;
    }
}