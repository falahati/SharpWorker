using Ionic.Zlib;
using SharpWorker.DataBackup;
using SharpWorker.DataStore;

namespace SharpWorker.NetCoordinator.DotNetZip
{
    internal class DotNetZipBackupArchiver : IDataBackupArchiver
    {
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Default;

        public bool Zip64 { get; set; } = false;

        /// <inheritdoc />
        public IDataArchive CreateNewArchive(string filename)
        {
            return new DotNetZipArchive(filename, CompressionLevel, Zip64);
        }

        /// <inheritdoc />
        public string FileExtension
        {
            get => ".zip";
        }
    }
}