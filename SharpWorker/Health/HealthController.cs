using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using SharpWorker.Log;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.Health
{
    [Description("Provides server and coordinator health information.")]
    [WebApiController("health")]
    // ReSharper disable once HollowTypeName
    public class HealthController : WebApiController
    {
        private readonly HealthWorker _healthWorker;
        private readonly Logger _logger;

        public HealthController(HealthWorker healthWorker, Logger logger)
        {
            _healthWorker = healthWorker;
            _logger = logger;
        }

        [Description("Gets the last updated health report.")]
        [WebApiResponse(HttpStatusCode.OK, typeof(HealthRecord))]
        [WebApiRequest(WebApiRequestMethod.Get, "", "read")]
        public WebApiResponse GetCurrentReport()
        {
            try
            {
                return Ok(_healthWorker.CurrentHealthReport);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(HealthController), nameof(WebApiResponse), LogType.Error, e);

                return InternalServerError(e);
            }
        }
    }
}