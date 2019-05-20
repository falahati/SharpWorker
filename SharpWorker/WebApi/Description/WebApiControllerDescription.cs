using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.WebApi.Description
{
    public class WebApiControllerDescription
    {
        public WebApiControllerDescription(Type controllerType, string controllerPath)
        {
            if (!typeof(WebApiController).IsAssignableFrom(controllerType))
            {
                throw new ArgumentException(nameof(controllerType));
            }

            ControllerType = controllerType;
            ControllerPath = controllerPath;
            Attributes = WebApiControllerAttribute.GetAttribute(controllerType);
            Actions = ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(info => info.DeclaringType == ControllerType)
                .Select(info => new WebApiControllerActionDescription(info))
                .Where(description => description.RequestAttributes?.Method != WebApiRequestMethod.Ignore)
                .ToArray();
            Name = ControllerType.Name;
            Description = ControllerType.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()?.Description;
            AccessScope = Attributes?.ScopeName;
        }

        public string AccessScope { get; }
        public WebApiControllerActionDescription[] Actions { get; }
        public WebApiControllerAttribute Attributes { get; }

        [JsonIgnore]
        public Type ControllerType { get; }

        public string ControllerTypeName
        {
            get => ControllerType.GetSimplifiedName();
        }

        public string ControllerPath { get; }
        public string Description { get; }
        public string Name { get; }
    }
}