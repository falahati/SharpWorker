using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWorker.DataStore;
using SharpWorker.Log;

namespace SharpWorker
{
    public interface IWorker : INotifyPropertyChanged, IDisposable
    {
        event EventHandler Terminated;
        long DataPoints { get; }
        DataTopic[] DataTopics { get; }

        [JsonIgnore]
        Logger Logger { get; }

        string Name { get; }
        WorkerScheduledAction[] GetScheduledActions();
        Task Start();
        Task Stop();
    }
}