using Newtonsoft.Json;

namespace SharpWorker.DataStore.Query
{
    public class DataStoreSortQuery
    {
        [JsonConstructor]
        public DataStoreSortQuery(IDataStoreQueryValue value, DataStoreSortQueryDirection direction)
        {
            Value = value;
            Direction = direction;
        }

        public DataStoreSortQueryDirection Direction { get; }

        public IDataStoreQueryValue Value { get; }
    }
}