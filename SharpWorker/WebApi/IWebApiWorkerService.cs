using SharpWorker.WebApi.Description;

namespace SharpWorker.WebApi
{
    public interface IWebApiWorkerService
    {
        WebApiControllerDescription[] GetApiDescription();
        WebApiController[] GetControllers();

        IWebApiWorker[] GetWorkers();

        bool RegisterController(
            WebApiController controller);

        bool RegisterController<T>() where T : WebApiController, new();

        bool RegisterWebApiWorker(
            IWebApiWorker worker);

        bool UnRegisterController(
            WebApiController controller);

        bool UnRegisterController<T>() where T : WebApiController;

        bool UnRegisterWebApiWorker(
            IWebApiWorker worker);
    }
}