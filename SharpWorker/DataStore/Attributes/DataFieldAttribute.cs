using System;
using System.Linq;
using System.Reflection;

namespace SharpWorker.DataStore.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DataFieldAttribute : Attribute
    {
        public DataFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        public DataFieldAttribute(bool ignore)
        {
            Ignored = ignore;
        }

        public string FieldName { get; }

        public bool Ignored { get; }

        public static DataFieldAttribute GetAttribute(MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttributes(typeof(DataFieldAttribute), true)
                .Cast<DataFieldAttribute>().FirstOrDefault();
        }
    }
}