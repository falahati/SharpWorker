using SharpWorker.DataStore;

namespace SharpWorker.DataBackup
{
    public interface IDataBackupArchiver
    {
        string FileExtension { get; }
        IDataArchive CreateNewArchive(string filename);
    }
}