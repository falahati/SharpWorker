using Newtonsoft.Json;

namespace SharpWorker.DataStore.Query
{
    public class DataStoreQueryField : IDataStoreQueryVariableValue
    {
        [JsonConstructor]
        public DataStoreQueryField(string fieldName)
        {
            FieldName = fieldName;
        }

        public string FieldName { get; }


        public static explicit operator DataStoreQueryField(string fieldName)
        {
            return new DataStoreQueryField(fieldName);
        }
    }
}