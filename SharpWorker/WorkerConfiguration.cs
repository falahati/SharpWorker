using System;
using Newtonsoft.Json;

namespace SharpWorker
{
    public class WorkerConfiguration
    {
        protected static Random Random = new Random();

        [JsonConstructor]
        public WorkerConfiguration(string workerType)
        {
            WorkerType = workerType;
        }

        public string Alias { get; set; } = null;
        public bool AutoStart { get; set; } = true;
        public WorkerOptions Options { get; set; } = null;
        public int StartDelay { get; set; } = Random.Next(5, 60);
        public string WorkerType { get; }

        public static WorkerConfiguration FromWorker<T>() where T : IWorker
        {
            return new WorkerConfiguration(typeof(T).GetSimplifiedName());
        }

        public static WorkerConfiguration FromWorker(Type type)
        {
            if (!typeof(IWorker).IsAssignableFrom(type))
            {
                throw new ArgumentException();
            }

            return new WorkerConfiguration(type.GetSimplifiedName());
        }

        public static string GetWorkerName(Type type)
        {
            return type.GetSimplifiedName();
        }
    }
}