using System;
using System.Linq;

namespace SharpWorker
{
    public static class TypeExtension
    {
        public static string GetSimplifiedName(this Type type)
        {
            if (type?.AssemblyQualifiedName != null)
            {
                return string.Join(",", type.AssemblyQualifiedName.Split(',').Take(2));
            }

            return type?.FullName;
        }

        public static Type GetTypeFromSimplifiedName(this string typeName)
        {
            return Type.GetType(typeName, true, true);
        }
    }
}