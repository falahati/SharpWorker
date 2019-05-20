using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using SharpWorker.Health;
using SharpWorker.Log;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;
using SharpWorker.WebApi.Description;

namespace SharpWorker
{
    // ReSharper disable once HollowTypeName
    [WebApiController("coordinator")]
    public class CoordinatorController : WebApiController
    {
        private readonly Coordinator _coordinator;
        private readonly Logger _logger;

        public CoordinatorController(Coordinator coordinator, Logger logger)
        {
            _coordinator = coordinator;
            _logger = logger;
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(string))]
        [WebApiResponse(HttpStatusCode.BadRequest)]
        [WebApiRequest(WebApiRequestMethod.Post, "Workers", "write")]
        public WebApiResponse AddWorker([WebApiFromBody] WorkerConfiguration configuration)
        {
            try
            {
                var newWorkerId = _coordinator.AddWorker(configuration);

                if (string.IsNullOrWhiteSpace(newWorkerId))
                {
                    return BadRequest();
                }

                return Ok(newWorkerId);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(ModifyWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }
        
        [WebApiResponse(HttpStatusCode.OK)]
        [WebApiResponse(HttpStatusCode.BadRequest)]
        [WebApiRequest(WebApiRequestMethod.Delete, "Workers/{workerId}", "write")]
        public WebApiResponse DeleteWorker(string workerId)
        {
            try
            {
                var newWorkerId = _coordinator.DeleteWorker(workerId);

                if (!newWorkerId)
                {
                    return BadRequest();
                }

                return Ok();
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(ModifyWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(WebApiControllerDescription[]))]
        [WebApiRequest(WebApiRequestMethod.Post, "WebAPI", "read")]
        public WebApiResponse GetWebApiDescription()
        {
            try
            {
                return Ok(_coordinator.WebApiService?.GetApiDescription());
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(ModifyWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }


        [WebApiResponse(HttpStatusCode.OK, typeof(CoordinatedWorker[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "Workers", "read")]
        public WebApiResponse GetWorkers()
        {
            try
            {
                return Ok(_coordinator?.Workers ?? new CoordinatedWorker[0]);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(GetWorkers), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [Description("Kills the server.")]
        [WebApiResponse(HttpStatusCode.OK, typeof(HealthRecord))]
        [WebApiRequest(WebApiRequestMethod.Delete, "", "kill")]
        public WebApiResponse KillApp()
        {
            try
            {
                var _ = Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    Environment.Exit(0);
                });

                return Ok();
            }
            catch (Exception e)
            {
                _logger.Log(nameof(HealthController), nameof(WebApiResponse), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(string))]
        [WebApiResponse(HttpStatusCode.BadRequest)]
        [WebApiRequest(WebApiRequestMethod.Put, "Workers/{workerId}", "write")]
        public WebApiResponse ModifyWorker(string workerId, [WebApiFromBody] WorkerConfiguration configuration)
        {
            try
            {
                var newWorkerId = _coordinator.ChangeWorkerConfiguration(workerId, configuration);

                if (string.IsNullOrWhiteSpace(newWorkerId))
                {
                    return BadRequest();
                }

                return Ok(newWorkerId);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(ModifyWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK)]
        [WebApiRequest(WebApiRequestMethod.Post, "Workers/{workerId}/Restart", "write")]
        public WebApiResponse RestartWorker(string workerId)
        {
            try
            {
                var _ = _coordinator.RestartWorker(workerId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(RestartWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK)]
        [WebApiRequest(WebApiRequestMethod.Post, "Workers/{workerId}/Start", "write")]
        public WebApiResponse StartWorker(string workerId)
        {
            try
            {
                var _ = _coordinator.StartWorker(workerId, false);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(StartWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK)]
        [WebApiRequest(WebApiRequestMethod.Post, "Workers/{workerId}/Stop", "write")]
        public WebApiResponse StopWorker(string workerId)
        {
            try
            {
                var _ = _coordinator.StopWorker(workerId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.Log(nameof(CoordinatorController), nameof(StopWorker), LogType.Error, e);

                return InternalServerError(e);
            }
        }
    }
}