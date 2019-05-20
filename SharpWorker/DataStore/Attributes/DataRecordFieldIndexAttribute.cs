using System;
using System.Linq;

namespace SharpWorker.DataStore.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DataRecordFieldIndexAttribute : Attribute
    {
        public DataRecordFieldIndexAttribute(string fieldName, bool isUnique)
        {
            FieldName = fieldName;
            IsUnique = isUnique;
        }

        public DataRecordFieldIndexAttribute(string fieldName) : this(fieldName, false)
        {
        }

        public string FieldName { get; }

        public bool IsUnique { get; set; }

        public static DataRecordFieldIndexAttribute[] GetAttributes(Type type)
        {
            return type.GetCustomAttributes(typeof(DataRecordFieldIndexAttribute), true)
                .Cast<DataRecordFieldIndexAttribute>().ToArray();
        }
    }
}