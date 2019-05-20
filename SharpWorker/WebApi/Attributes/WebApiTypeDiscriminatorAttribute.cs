using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SharpWorker.WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class WebApiTypeDiscriminatorAttribute : Attribute
    {
        public WebApiTypeDiscriminatorAttribute(params Type[] knowTypes)
        {
            KnowTypes = knowTypes;
        }

        public WebApiTypeDiscriminatorAttribute()
        {
            KnowTypes = null;
        }

        private Type[] KnowTypes { get; }

        public static Type GetBaseType(Type type)
        {
            var t = type;

            while (t.BaseType != null && t.BaseType.Name != nameof(Object))
            {
                if (HasAttribute(t.BaseType, false))
                {
                    return t.BaseType;
                }

                t = t.BaseType;
            }

            foreach (var @interface in type.GetInterfaces())
            {
                if (HasAttribute(@interface, false))
                {
                    return @interface;
                }
            }

            return null;
        }

        public static Type[] GetSubtypes(Type type)
        {
            var attribute = GetAttribute(type, false);

            if (attribute != null && !type.IsSealed && !type.IsPrimitive)
            {
                if (attribute.KnowTypes != null)
                {
                    return attribute.KnowTypes;
                }

                return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t != type && type.IsAssignableFrom(t))
                    .ToArray();
            }

            return new Type[0];
        }

        public static bool HasAttribute(Type type, bool inherit)
        {
            return GetAttribute(type, inherit) != null;
        }

        private static WebApiTypeDiscriminatorAttribute GetAttribute(Type type, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(typeof(WebApiTypeDiscriminatorAttribute), inherit)
                .Cast<WebApiTypeDiscriminatorAttribute>().FirstOrDefault();
        }
    }
}