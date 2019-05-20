using System;
using System.IO;

namespace SharpWorker.DataBackup
{
    public class DataBackupStatus
    {
        private readonly FileInfo _fileInfo;

        public DataBackupStatus(FileInfo file, DataBackupState state)
        {
            _fileInfo = file;
            State = state;
        }

        public DateTime DateTime
        {
            get => _fileInfo.CreationTimeUtc;
        }

        public string FileName
        {
            get => _fileInfo.Name;
        }

        public long FileSize
        {
            get => _fileInfo.Length;
        }

        public DataBackupState State { get; }

        public FileInfo GetFile()
        {
            return _fileInfo;
        }
    }
}