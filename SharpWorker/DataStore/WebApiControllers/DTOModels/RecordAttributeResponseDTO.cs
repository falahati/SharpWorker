using System.Collections.Generic;
using SharpWorker.DataValue;

namespace SharpWorker.DataStore.WebApiControllers.DTOModels
{
    public class RecordAttributeResponseDTO
    {
        public int Count { get; set; }
        public long? End { get; set; }
        public long? Start { get; set; }
        public IEnumerable<DataValueHolder> Values { get; set; }
    }
}