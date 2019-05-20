using Newtonsoft.Json;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker
{
    public interface ICustomizableWorker : IWorker
    {
        [JsonIgnore]
        WorkerOptions Options { get; }
    }
}