using System;
using System.Linq;

namespace SharpWorker.DataStore.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DataRecordExpressionIndexAttribute : Attribute
    {
        public DataRecordExpressionIndexAttribute(string indexName, string expression) : this(indexName, expression,
            false)
        {
        }

        public DataRecordExpressionIndexAttribute(string indexName, string expression, bool isUnique)
        {
            IndexName = indexName;
            IsUnique = isUnique;
            Expression = expression;
        }

        public string Expression { get; }

        public string IndexName { get; }

        public bool IsUnique { get; set; }

        public static DataRecordExpressionIndexAttribute[] GetAttributes(Type type)
        {
            return type.GetCustomAttributes(typeof(DataRecordExpressionIndexAttribute), true)
                .Cast<DataRecordExpressionIndexAttribute>().ToArray();
        }
    }
}