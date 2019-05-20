using Newtonsoft.Json;
using SharpWorker.DataStore.Query;
using SharpWorker.DataValue;

namespace SharpWorker.DataStore.WebApiControllers.DTOModels
{
    public class RecordAttributeGranularityRequestDTO
    {
        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public RecordAttributeGranularityRequestDTO(
            DataStoreQueryCalculatedValue condition,
            long? granularityDuration,
            DataGranularityMode granularityMode,
            DataGranularityFillMode granularityFill)
        {
            Condition = condition;
            GranularityDuration = granularityDuration;
            GranularityMode = granularityMode;
            GranularityFill = granularityFill;
        }

        public DataStoreQueryCalculatedValue Condition { get; }
        public long? GranularityDuration { get; }
        public DataGranularityFillMode GranularityFill { get; }
        public DataGranularityMode GranularityMode { get; }
    }
}