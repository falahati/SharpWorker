using Newtonsoft.Json;

namespace SharpWorker.DataStore.Query
{
    public class DataStoreQueryExpressionIndex : IDataStoreQueryVariableValue
    {
        [JsonConstructor]
        public DataStoreQueryExpressionIndex(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public static explicit operator DataStoreQueryExpressionIndex(string fieldName)
        {
            return new DataStoreQueryExpressionIndex(fieldName);
        }
    }
}