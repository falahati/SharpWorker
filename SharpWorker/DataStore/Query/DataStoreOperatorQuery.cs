using Newtonsoft.Json;

namespace SharpWorker.DataStore.Query
{
    public class DataStoreOperatorQuery : DataStoreQueryCalculatedValue
    {
        [JsonConstructor]
        public DataStoreOperatorQuery(
            DataStoreQueryCalculatedValue left,
            DataStoreQueryCalculatedValue right,
            DataStoreOperatorQueryType type)
        {
            Left = left;
            Right = right;
            Type = type;
        }

        public DataStoreQueryCalculatedValue Left { get; }
        public DataStoreQueryCalculatedValue Right { get; }
        public DataStoreOperatorQueryType Type { get; set; }
    }
}