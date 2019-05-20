using System.Collections.Generic;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using SharpWorker.DataStore;

namespace SharpWorker.DataBackup.SharpZip
{
    public class SharpZipArchive : IDataArchive
    {
        private readonly List<string> _files = new List<string>();

        public SharpZipArchive(string fileName, CompressionLevel compressionLevel)
        {
            FileName = fileName;
            FileStream = File.Open(FileName, FileMode.Create);
            ZipWriter = WriterFactory.Open(FileStream, ArchiveType.Zip,
                new ZipWriterOptions(CompressionType.Deflate)
                {
                    DeflateCompressionLevel = compressionLevel
                });
        }

        public FileStream FileStream { get; set; }
        public IWriter ZipWriter { get; set; }

        /// <inheritdoc />
        public void AddFile(string fileAddress, string fileName)
        {
            using (var fileToAdd = File.Open(fileAddress, FileMode.Open))
            {
                ZipWriter.Write(fileName, fileToAdd, null);
                FileStream.Flush(true);
            }

            _files.Add(fileName);
        }

        /// <inheritdoc />
        public void Close()
        {
            lock (this)
            {
                ZipWriter?.Dispose();
                FileStream?.Flush(true);
                FileStream?.Close();
                FileStream?.Dispose();
                FileStream = null;
                ZipWriter = null;
            }
        }

        /// <inheritdoc />
        public string FileName { get; }

        /// <inheritdoc />
        public bool HasFile(string fileName)
        {
            return _files.Contains(fileName);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Close();
        }
    }
}