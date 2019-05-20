using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;
using SharpWorker.WebApi.Description;

namespace SharpWorker.WebApi.Description
{
    public class WebApiControllerActionDescription
    {
        public WebApiControllerActionDescription(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            RequestAttributes = WebApiRequestAttribute.GetAttribute(methodInfo);
            ResponseAttributes = WebApiResponseAttribute.GetAttributes(methodInfo);
            Parameters = MethodInfo.GetParameters()
                .Select(info => new WebApiControllerActionParameterDescription(info))
                .ToArray();
            Name = MethodInfo.Name;
            Description = MethodInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()?.Description;
            AccessScope = WebApiController.GetAccessScope(MethodInfo);
        }

        public string AccessScope { get; }
        public string Description { get; }

        [JsonIgnore]
        public MethodInfo MethodInfo { get; }
        public string Name { get; }
        public WebApiControllerActionParameterDescription[] Parameters { get; }
        public WebApiRequestAttribute RequestAttributes { get; }
        public WebApiResponseAttribute[] ResponseAttributes { get; }
    }
}