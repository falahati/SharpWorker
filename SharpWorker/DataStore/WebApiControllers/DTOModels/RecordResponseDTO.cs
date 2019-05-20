using System.Collections.Generic;

namespace SharpWorker.DataStore.WebApiControllers.DTOModels
{
    public class RecordResponseDTO
    {
        public int Count { get; set; }
        public long? End { get; set; }
        public long? Start { get; set; }
        public IEnumerable<DataRecord> Values { get; set; }
    }
}