using System.Net;

namespace SharpWorker.WebApi
{
    public class WebApiResponse<T> : WebApiResponse
    {
        public WebApiResponse(HttpStatusCode responseCode, T content) : base(responseCode, content)
        {
            Content = content;
            Type = typeof(T);
        }

        public new T Content { get; }
    }
}