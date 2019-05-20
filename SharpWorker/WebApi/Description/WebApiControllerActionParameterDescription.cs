using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.WebApi.Description
{
    public class WebApiControllerActionParameterDescription
    {
        public WebApiControllerActionParameterDescription(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
            IsFromBody = WebApiFromBodyAttribute.GetAttribute(parameterInfo) != null;
            IsOptional = parameterInfo.IsOptional;
            Type = parameterInfo.ParameterType;
            Name = parameterInfo.Name;
            Description = parameterInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()?.Description;
        }

        public string Description { get; }
        public bool IsFromBody { get; }
        public bool IsOptional { get; }
        public string Name { get; }

        [JsonIgnore]
        public ParameterInfo ParameterInfo { get; }

        [JsonIgnore]
        public Type Type { get; }

        public string TypeName
        {
            get => Type.FullName;
        }
    }
}