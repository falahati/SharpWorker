using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Results;
using SharpWorker.WebApi;

namespace SharpWorker.NetCoordinator.WebApi
{
    public abstract class WebApiProxyControllerBase : ApiController
    {
        protected WebApiProxyControllerBase(WebApiController controller)
        {
            WebApiController = controller;
        }

        protected WebApiController WebApiController { get; }

        /// <inheritdoc />
        // ReSharper disable once FlagArgument
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WebApiController.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            string[] claimedScopes = null;

            if (controllerContext?.Request?.Headers?.Authorization?.Scheme?.Trim()
                    .Equals("Bearer", StringComparison.InvariantCultureIgnoreCase) ==
                true)
            {
                var jwtToken = controllerContext.Request?.Headers?.Authorization?.Parameter?.Trim();

                if (!string.IsNullOrWhiteSpace(jwtToken))
                {
                    claimedScopes = WebApiJWTPayload.GetPayload(jwtToken)?.ClaimedScopes;
                }
            }

            base.Initialize(controllerContext);
            WebApiController.Initialize(
                new WebApiRequest(
                    controllerContext?.Request,
                    controllerContext?.RequestContext?.IsLocal ?? false,
                    claimedScopes ?? new string[0]
                )
            );
        }

        protected internal IHttpActionResult Invoke(string actionName, params object[] parameters)
        {
            var response = WebApiController.Invoke(actionName, parameters);

            if (response.Type == typeof(HttpResponseMessage))
            {
                return ResponseMessage(response.Content as HttpResponseMessage);
            }

            if (response.Type != null)
            {
                var negotiatedContentResultConstructor = typeof(NegotiatedContentResult<>)
                    .MakeGenericType(response.Type)
                    .GetConstructor(new[] {typeof(HttpStatusCode), response.Type, typeof(ApiController)});

                if (negotiatedContentResultConstructor != null)
                {
                    return (IHttpActionResult) negotiatedContentResultConstructor.Invoke(new[]
                        {response.ResponseCode, response.Content, this});
                }
            }

            return new NegotiatedContentResult<string>(response.ResponseCode, response.Content.ToString(), this);
        }
    }
}