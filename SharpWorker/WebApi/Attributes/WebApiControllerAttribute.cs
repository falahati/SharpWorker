using System;
using System.Linq;
using Newtonsoft.Json;

namespace SharpWorker.WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class WebApiControllerAttribute : Attribute
    {
        public WebApiControllerAttribute(string accessScopeName)
        {
            if (accessScopeName != null &&
                (string.IsNullOrWhiteSpace(accessScopeName) || accessScopeName.Contains(" ")))
            {
                throw new ArgumentException(nameof(accessScopeName));
            }

            ScopeName = accessScopeName?.ToLower().Trim();
        }

        [JsonIgnore]
        public override object TypeId {
            get => base.TypeId;
        }

        public string ScopeName { get; }

        public static WebApiControllerAttribute GetAttribute(Type type)
        {
            return type.GetCustomAttributes(typeof(WebApiControllerAttribute), true).Cast<WebApiControllerAttribute>()
                .FirstOrDefault();
        }
    }
}