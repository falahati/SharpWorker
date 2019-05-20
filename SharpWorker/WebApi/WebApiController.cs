using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.WebApi
{
    // ReSharper disable once HollowTypeName
    // ReSharper disable once ClassTooBig
    public abstract class WebApiController : IDisposable
    {
        protected HttpRequestMessage Request
        {
            get => RequestContext?.RequestMessage;
        }

        protected WebApiRequest RequestContext { get; private set; }

        public virtual void Dispose()
        {
        }

        internal static string GetAccessScope(MethodInfo methodInfo)
        {
            var controllerAccessAttribute = WebApiControllerAttribute.GetAttribute(methodInfo.ReflectedType);
            var actionRequestAttribute = WebApiRequestAttribute.GetAttribute(methodInfo);

            string scopeName = null;

            if (!string.IsNullOrWhiteSpace(controllerAccessAttribute?.ScopeName) &&
                !string.IsNullOrWhiteSpace(actionRequestAttribute?.AccessScope))
            {
                scopeName = actionRequestAttribute.AccessScope + ":" + controllerAccessAttribute.ScopeName;
            }
            else if (!string.IsNullOrWhiteSpace(controllerAccessAttribute?.ScopeName))
            {
                scopeName = controllerAccessAttribute.ScopeName;
            }
            else if (!string.IsNullOrWhiteSpace(actionRequestAttribute?.AccessScope))
            {
                scopeName = actionRequestAttribute.AccessScope;
            }

            return scopeName;
        }

        public virtual void Initialize(WebApiRequest request)
        {
            RequestContext = request;
        }

        public WebApiResponse Invoke(string actionName, params object[] parameters)
        {
            var webApiActionMethod = GetType().GetMethod(actionName, parameters.Select(o => o.GetType()).ToArray());

            if (webApiActionMethod == null)
            {
                return NotFound();
            }

            var accessScope = GetAccessScope(webApiActionMethod);

            if (!string.IsNullOrWhiteSpace(accessScope) &&
                RequestContext?.ClaimedScopes?.Any(s => s == "*") != true &&
                RequestContext?.ClaimedScopes?.Any(s =>
                    s.Equals(accessScope, StringComparison.InvariantCultureIgnoreCase)
                ) !=
                true)
            {
                return Unauthorized();
            }

            var response = webApiActionMethod.Invoke(this, parameters);

            if (webApiActionMethod.ReturnType == typeof(void) || response == null)
            {
                return Ok();
            }

            if (response is WebApiResponse)
            {
                return response as WebApiResponse;
            }

            return GetType().GetMethods().FirstOrDefault(info => info.Name == nameof(Ok) && info.IsGenericMethod)
                ?.MakeGenericMethod(webApiActionMethod.ReturnType).Invoke(this, new[] {response}) as WebApiResponse;
        }

        protected virtual WebApiResponse BadRequest()
        {
            return new WebApiResponse(HttpStatusCode.BadRequest);
        }

        protected virtual WebApiResponse BadRequest(string message)
        {
            return new WebApiResponse<string>(HttpStatusCode.BadRequest, message);
        }

        protected virtual WebApiResponse Conflict()
        {
            return new WebApiResponse(HttpStatusCode.Conflict);
        }

        protected virtual WebApiResponse Content<T>(HttpStatusCode statusCode, T value)
        {
            return new WebApiResponse<T>(statusCode, value);
        }

        protected WebApiResponse Created<T>(string location, T content)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return Created(new Uri(location, UriKind.RelativeOrAbsolute), content);
        }

        protected WebApiResponse Created<T>(Uri location, T content)
        {
            return new WebApiResponse<T>(HttpStatusCode.Created, content)
            {
                Headers =
                {
                    Location = location
                }
            };
        }

        protected WebApiResponse InternalServerError()
        {
            return new WebApiResponse(HttpStatusCode.InternalServerError);
        }

        protected WebApiResponse InternalServerError(Exception exception)
        {
            return new WebApiResponse(HttpStatusCode.InternalServerError, exception.Message);
        }

        protected WebApiResponse NotFound()
        {
            return new WebApiResponse(HttpStatusCode.NotFound);
        }

        // ReSharper disable once MethodNameNotMeaningful
        protected WebApiResponse Ok()
        {
            return new WebApiResponse(HttpStatusCode.OK);
        }

        // ReSharper disable once MethodNameNotMeaningful
        protected WebApiResponse Ok<T>(T content)
        {
            return new WebApiResponse<T>(HttpStatusCode.OK, content);
        }

        protected virtual WebApiResponse Redirect(string location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return Redirect(new Uri(location));
        }

        protected virtual WebApiResponse Redirect(Uri location)
        {
            return new WebApiResponse(HttpStatusCode.Redirect)
            {
                Headers =
                {
                    Location = location
                }
            };
        }

        protected virtual WebApiResponse ResponseMessage(HttpResponseMessage response)
        {
            return new WebApiResponse(response);
        }

        protected virtual WebApiResponse StatusCode(HttpStatusCode status)
        {
            return new WebApiResponse(status);
        }

        protected WebApiResponse Unauthorized(params AuthenticationHeaderValue[] challenges)
        {
            return Unauthorized((IEnumerable<AuthenticationHeaderValue>) challenges);
        }

        protected virtual WebApiResponse Unauthorized(IEnumerable<AuthenticationHeaderValue> challenges)
        {
            var response = new WebApiResponse(HttpStatusCode.Unauthorized);

            foreach (var authenticationHeaderValue in challenges)
            {
                response.Headers.WwwAuthenticate.Add(authenticationHeaderValue);
            }

            return response;
        }
    }
}