using System.Net.Http;

namespace SharpWorker.WebApi
{
    public class WebApiRequest
    {
        public WebApiRequest(HttpRequestMessage requestMessage, bool isLocal, string[] claimedScopes)
        {
            RequestMessage = requestMessage;
            IsLocal = isLocal;
            ClaimedScopes = claimedScopes ?? new string[0];
        }

        public string[] ClaimedScopes { get; }
        public bool IsLocal { get; }
        public HttpRequestMessage RequestMessage { get; }
    }
}