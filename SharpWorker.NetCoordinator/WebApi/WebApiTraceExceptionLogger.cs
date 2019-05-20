using System.Diagnostics;
using System.Web.Http.ExceptionHandling;
using SharpWorker.Log;

namespace SharpWorker.NetCoordinator.WebApi
{
    internal class WebApiTraceExceptionLogger : ExceptionLogger
    {
        private readonly Logger _logger;

        public WebApiTraceExceptionLogger(Logger logger)
        {
            _logger = logger;
        }

        public override void Log(ExceptionLoggerContext context)
        {
#if DEBUG
            Trace.TraceError(context.ExceptionContext.Exception.ToString());
#endif
            _logger.Log(nameof(WebApiTraceExceptionLogger), nameof(Log), LogType.Error, context.ExceptionContext.Exception);
        }
    }
}