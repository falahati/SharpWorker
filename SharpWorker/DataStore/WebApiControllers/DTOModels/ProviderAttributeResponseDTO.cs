using Newtonsoft.Json;
using SharpWorker.DataValue;

namespace SharpWorker.DataStore.WebApiControllers.DTOModels
{
    public class ProviderAttributeResponseDTO
    {
        [JsonConstructor]
        public ProviderAttributeResponseDTO(string name, DataValueType type)
        {
            AttributeName = name;
            AttributeType = type;
        }

        public string AttributeName { get; }
        public DataValueType AttributeType { get; }
    }
}