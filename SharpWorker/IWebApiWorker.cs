using SharpWorker.WebApi;

namespace SharpWorker
{
    public interface IWebApiWorker : IWorker
    {
        WebApiController[] GetWebApiControllers();
    }
}