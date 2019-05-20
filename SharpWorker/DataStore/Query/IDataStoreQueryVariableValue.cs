using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataStore.Query
{
    [WebApiTypeDiscriminator]
    public interface IDataStoreQueryVariableValue : IDataStoreQueryValue
    {
    }
}