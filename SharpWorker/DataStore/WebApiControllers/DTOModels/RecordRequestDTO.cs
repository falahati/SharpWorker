using Newtonsoft.Json;
using SharpWorker.DataStore.Query;

namespace SharpWorker.DataStore.WebApiControllers.DTOModels
{
    public class RecordRequestDTO
    {
        [JsonConstructor]
        public RecordRequestDTO(DataStoreQueryCalculatedValue condition, DataStoreSortQuery sort)
        {
            Condition = condition;
            Sort = sort;
        }

        public DataStoreQueryCalculatedValue Condition { get; }

        public DataStoreSortQuery Sort { get; }
    }
}