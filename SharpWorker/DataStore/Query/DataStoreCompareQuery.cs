using Newtonsoft.Json;

namespace SharpWorker.DataStore.Query
{
    public class DataStoreCompareQuery : DataStoreQueryCalculatedValue
    {
        [JsonConstructor]
        public DataStoreCompareQuery(
            IDataStoreQueryVariableValue fieldOrIndex,
            DataStoreQueryConstant value,
            DataStoreCompareQueryType type)
        {
            FieldOrIndex = fieldOrIndex;
            Value = value;
            Type = type;
        }

        public IDataStoreQueryVariableValue FieldOrIndex { get; }
        public DataStoreCompareQueryType Type { get; }
        public DataStoreQueryConstant Value { get; }
    }
}