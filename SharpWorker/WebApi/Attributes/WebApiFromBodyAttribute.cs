using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SharpWorker.WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class WebApiFromBodyAttribute : Attribute
    {
        [JsonIgnore]
        public override object TypeId
        {
            get => base.TypeId;
        }
        public static WebApiFromBodyAttribute GetAttribute(ParameterInfo parameterInfo)
        {
            return parameterInfo.GetCustomAttributes(typeof(WebApiFromBodyAttribute), true)
                .Cast<WebApiFromBodyAttribute>().FirstOrDefault();
        }
    }
}