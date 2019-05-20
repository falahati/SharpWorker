using System.IO;

namespace SharpWorker.Health.Models
{
    public class DriveHealth
    {
        public DriveHealth(DriveInfo drive)
        {
            Name = drive.Name;
            Path = drive.RootDirectory.FullName;
            TotalSpaceBytes = drive.TotalSize;
            FreeSpaceBytes = drive.AvailableFreeSpace;
        }

        public DriveHealth()
        {
        }

        public long FreeSpaceBytes { get; }
        public string Name { get; }
        public string Path { get; }
        public long TotalSpaceBytes { get; }
    }
}