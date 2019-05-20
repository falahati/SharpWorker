using System.Collections.Generic;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;
using SharpWorker.DataStore;

namespace SharpWorker.NetCoordinator.DotNetZip
{
    internal class DotNetZipArchive : IDataArchive
    {
        private readonly List<string> _addedFiles = new List<string>();

        public DotNetZipArchive(string fileName, CompressionLevel compressionLevel, bool zip64)
        {
            FileName = fileName;
            Zip64 = zip64;
            ZipFile = new ZipFile(FileName, Encoding.UTF8)
            {
                CompressionMethod = CompressionMethod.Deflate,
                CompressionLevel = compressionLevel,
                UseZip64WhenSaving = zip64 ? Zip64Option.AsNecessary : Zip64Option.Default
            };
        }

        public bool Zip64 { get; }

        public ZipFile ZipFile { get; }

        /// <inheritdoc />
        public void AddFile(string fileAddress, string fileName)
        {
            lock (this)
            {
                var entry = ZipFile.AddFile(fileAddress);
                entry.FileName = fileName;
                ZipFile?.Save();
                _addedFiles.Add(fileName);
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            lock (this)
            {
                ZipFile?.Save();
                ZipFile?.Dispose();
            }
        }

        /// <inheritdoc />
        public string FileName { get; }

        /// <inheritdoc />
        public bool HasFile(string fileName)
        {
            lock (this)
            {
                return _addedFiles.Contains(fileName);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Close();
        }
    }
}