using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataStore.Query
{
    [WebApiTypeDiscriminator]
    public abstract class DataStoreQueryCalculatedValue : IDataStoreQueryValue
    {
        public static DataStoreQueryCalculatedValue operator &(DataStoreQueryCalculatedValue left, DataStoreQueryCalculatedValue right)
        {
            return new DataStoreOperatorQuery(left, right, DataStoreOperatorQueryType.And);
        }

        public static DataStoreQueryCalculatedValue operator |(DataStoreQueryCalculatedValue left, DataStoreQueryCalculatedValue right)
        {
            return new DataStoreOperatorQuery(left, right, DataStoreOperatorQueryType.Or);
        }
    }
}