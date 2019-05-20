using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SharpWorker.WebApi
{
    public class WebApiResponse
    {
        private HttpResponseHeaders _headers;
        private Type _type;

        public WebApiResponse(HttpResponseMessage httpResponseMessage) : this(httpResponseMessage.StatusCode,
            httpResponseMessage)
        {
            Headers = httpResponseMessage.Headers;
            Type = typeof(HttpResponseMessage);
        }

        public WebApiResponse(HttpStatusCode responseCode)
        {
            ResponseCode = responseCode;
            Content = null;
        }

        public WebApiResponse(HttpStatusCode responseCode, object content) : this(responseCode)
        {
            Content = content;
        }

        public object Content { get; }

        public HttpResponseHeaders Headers
        {
            get => _headers ?? (_headers = new HttpResponseMessage().Headers);
            protected set => _headers = value;
        }

        public HttpStatusCode ResponseCode { get; }

        public Type Type
        {
            get => _type ?? Content?.GetType() ?? typeof(string);
            protected set => _type = value;
        }
    }
}