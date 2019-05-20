using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace SharpWorker.WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class WebApiResponseAttribute : Attribute
    {
        [JsonIgnore]
        public override object TypeId
        {
            get => base.TypeId;
        }
        public WebApiResponseAttribute(Type responseType)
        {
            ResponseType = responseType;
            ResponseCode = HttpStatusCode.OK;
        }

        public WebApiResponseAttribute(HttpStatusCode responseCode) : this(null)
        {
            ResponseCode = responseCode;
        }

        public WebApiResponseAttribute(HttpStatusCode responseCode, Type responseType) : this(responseType)
        {
            ResponseCode = responseCode;
        }

        public HttpStatusCode ResponseCode { get; }
        [JsonIgnore]
        public Type ResponseType { get; }

        public string ResponseTypeName
        {
            get => ResponseType.GetSimplifiedName();
        }

        public static WebApiResponseAttribute[] GetAttributes(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(WebApiResponseAttribute), true)
                .Cast<WebApiResponseAttribute>().ToArray();
        }
    }
}