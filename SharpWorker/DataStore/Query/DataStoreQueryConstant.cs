using System.Collections;
using Newtonsoft.Json;

namespace SharpWorker.DataStore.Query
{
#pragma warning disable 660,661
    public class DataStoreQueryConstant
#pragma warning restore 660,661
    {
        [JsonConstructor]
        public DataStoreQueryConstant(dynamic value)
        {
            Value = value;
        }

        public DataStoreQueryConstant(dynamic[] values)
        {
            Value = values;
        }

        public bool IsArray
        {
            get => Value is IEnumerable;
        }

        public dynamic Value { get; }

        public static DataStoreCompareQuery operator ==(DataStoreQueryConstant left, IDataStoreQueryVariableValue right)
        {
            return new DataStoreCompareQuery(right, left, DataStoreCompareQueryType.Equal);
        }

        public static DataStoreCompareQuery operator ==(IDataStoreQueryVariableValue left, DataStoreQueryConstant right)
        {
            return new DataStoreCompareQuery(left, right, DataStoreCompareQueryType.Equal);
        }

        public static explicit operator DataStoreQueryConstant(bool value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(bool? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(byte value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(byte? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(short value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(short? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(ushort value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(ushort? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(int value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(int? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(uint value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(uint? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(long value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(long? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(ulong value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(ulong? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(string value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(double value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(double? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(decimal value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(decimal? value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(float value)
        {
            return new DataStoreQueryConstant(value);
        }

        public static explicit operator DataStoreQueryConstant(float? value)
        {
            return new DataStoreQueryConstant(value);
        }
        

        public static DataStoreCompareQuery operator >(DataStoreQueryConstant left, IDataStoreQueryVariableValue right)
        {
            return new DataStoreCompareQuery(right, left, DataStoreCompareQueryType.Greater);
        }

        public static DataStoreCompareQuery operator >(IDataStoreQueryVariableValue left, DataStoreQueryConstant right)
        {
            return new DataStoreCompareQuery(left, right, DataStoreCompareQueryType.Greater);
        }

        public static DataStoreCompareQuery operator >=(DataStoreQueryConstant left, IDataStoreQueryVariableValue right)
        {
            return new DataStoreCompareQuery(right, left, DataStoreCompareQueryType.EqualOrGreater);
        }

        public static DataStoreCompareQuery operator >=(IDataStoreQueryVariableValue left, DataStoreQueryConstant right)
        {
            return new DataStoreCompareQuery(left, right, DataStoreCompareQueryType.EqualOrGreater);
        }

        public static DataStoreCompareQuery operator !=(DataStoreQueryConstant left, IDataStoreQueryVariableValue right)
        {
            return new DataStoreCompareQuery(right, left, DataStoreCompareQueryType.NotEqual);
        }

        public static DataStoreCompareQuery operator !=(IDataStoreQueryVariableValue left, DataStoreQueryConstant right)
        {
            return new DataStoreCompareQuery(left, right, DataStoreCompareQueryType.NotEqual);
        }

        public static DataStoreCompareQuery operator <(DataStoreQueryConstant left, IDataStoreQueryVariableValue right)
        {
            return new DataStoreCompareQuery(right, left, DataStoreCompareQueryType.Less);
        }

        public static DataStoreCompareQuery operator <(IDataStoreQueryVariableValue left, DataStoreQueryConstant right)
        {
            return new DataStoreCompareQuery(left, right, DataStoreCompareQueryType.Less);
        }

        public static DataStoreCompareQuery operator <=(DataStoreQueryConstant left, IDataStoreQueryVariableValue right)
        {
            return new DataStoreCompareQuery(right, left, DataStoreCompareQueryType.NotEqual);
        }

        public static DataStoreCompareQuery operator <=(IDataStoreQueryVariableValue left, DataStoreQueryConstant right)
        {
            return new DataStoreCompareQuery(left, right, DataStoreCompareQueryType.NotEqual);
        }
    }
}