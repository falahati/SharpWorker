using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.Log
{
    // ReSharper disable once HollowTypeName
    [WebApiController("log")]
    public class LoggerController : WebApiController
    {
        private readonly Logger _logger;

        public LoggerController(Logger logger)
        {
            _logger = logger;
        }


        [WebApiResponse(HttpStatusCode.OK, typeof(byte[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "Download")]
        public WebApiResponse DownloadLogFile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_logger.Filename) || !File.Exists(_logger.Filename))
                {
                    BadRequest();
                }

                var tmpFile = Path.GetTempFileName();

                File.Copy(_logger.Filename, tmpFile, true);

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new FileStream(tmpFile, FileMode.Open, FileAccess.Read))
                };

                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = Path.GetFileName(_logger.Filename)
                    };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                return ResponseMessage(response);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(LoggerController), nameof(DownloadLogFile), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(LogRecord[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "")]
        public WebApiResponse GetGlobalHistory()
        {
            try
            {
                return Ok(_logger.GlobalHistory);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(LoggerController), nameof(GetGlobalHistory), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(LogRecord[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "Workers/{workerName}/History")]
        public WebApiResponse GetWorkerHistory(string workerName)
        {
            try
            {
                return Ok(_logger.GetWorkerHistory(workerName));
            }
            catch (Exception e)
            {
                _logger.Log(nameof(LoggerController), nameof(GetWorkerHistory), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(LogRecord[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "Workers/{workerName}/Records")]
        public WebApiResponse GetWorkerRecords(string workerName)
        {
            try
            {
                return Ok(_logger.GetWorkerRecords(workerName));
            }
            catch (Exception e)
            {
                _logger.Log(nameof(LoggerController), nameof(GetWorkerRecords), LogType.Error, e);

                return InternalServerError(e);
            }
        }


        [WebApiResponse(HttpStatusCode.OK, typeof(string[]))]
        [WebApiRequest(WebApiRequestMethod.Get, "Workers")]
        public WebApiResponse GetWorkers()
        {
            try
            {
                return Ok(_logger.GetWorkers());
            }
            catch (Exception e)
            {
                _logger.Log(nameof(LoggerController), nameof(GetWorkers), LogType.Error, e);

                return InternalServerError(e);
            }
        }
    }
}