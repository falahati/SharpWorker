using System;

namespace SharpWorker.DataStore
{
    public interface IDataArchive : IDisposable
    {
        string FileName { get; }
        void AddFile(string fileAddress, string fileName);

        void Close();

        bool HasFile(string fileName);
    }
}