using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SharpWorker.WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class WebApiRequestAttribute : Attribute
    {
        [JsonIgnore]
        public override object TypeId
        {
            get => base.TypeId;
        }
        public WebApiRequestAttribute(WebApiRequestMethod method, string routeTemplate, string accessScope)
        {
            Method = method;
            RouteTemplate = routeTemplate;
            if (accessScope != null &&
                (string.IsNullOrWhiteSpace(accessScope) || accessScope.Contains(" ")))
            {
                throw new ArgumentException(nameof(accessScope));
            }
            AccessScope = accessScope?.ToLower().Trim();
        }

        public WebApiRequestAttribute(string template, string accessScope) : this(WebApiRequestMethod.Auto, template,
            accessScope)
        {
        }

        public WebApiRequestAttribute(WebApiRequestMethod method, string routeTemplate) : this(method, routeTemplate, null)
        {
        }

        public WebApiRequestAttribute(WebApiRequestMethod method) : this(method, null, null)
        {
        }

        public WebApiRequestAttribute(string template) : this(WebApiRequestMethod.Auto, template, null)
        {
        }

        public WebApiRequestMethod Method { get; }
        public string AccessScope { get; }
        public string RouteTemplate { get; }
        
        public static WebApiRequestAttribute GetAttribute(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(WebApiRequestAttribute), true)
                .Cast<WebApiRequestAttribute>().FirstOrDefault();
        }
    }
}