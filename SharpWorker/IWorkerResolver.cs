using System;
using System.Reflection;
using SharpWorker.DataStore;
using SharpWorker.Log;

namespace SharpWorker
{
    public interface IWorkerResolver
    {
        Assembly[] GetAssemblies();

        Type[] GetWorkerTypes();

        // ReSharper disable once TooManyArguments
        IWorker ActivateWorker(
            Coordinator coordinator,
            DataStoreBase dataStore,
            Logger logger,
            WorkerConfiguration configuration);
    }
}