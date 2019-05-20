using SharpCompress.Compressors.Deflate;
using SharpWorker.DataStore;

namespace SharpWorker.DataBackup.SharpZip
{
    public class SharpZipArchiver : IDataBackupArchiver
    {
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Default;

        /// <inheritdoc />
        public IDataArchive CreateNewArchive(string filename)
        {
            return new SharpZipArchive(filename, CompressionLevel);
        }

        /// <inheritdoc />
        public string FileExtension
        {
            get => ".zip";
        }
    }
}