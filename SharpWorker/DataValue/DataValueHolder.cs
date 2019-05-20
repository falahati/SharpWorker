using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SharpWorker.DataValue
{
    public class DataValueHolder : IEquatable<DataValueHolder>
    {
        public DataValueHolder(DateTime time)
        {
            Timestamp = time.Ticks;
        }

        public DataValueHolder(DateTime time, double? numberValue) : this(time)
        {
            NumberValue = numberValue;
        }

        public DataValueHolder(DateTime time, object objectValue) : this(time)
        {
            ObjectValue = objectValue;
        }

        public DataValueHolder(DateTime time, string stringValue) : this(time)
        {
            StringValue = stringValue;
        }

        public DataValueHolder(DateTime time, Aggregated aggregatedValue) : this(time)
        {
            AggregatedValue = aggregatedValue;
        }

        public DataValueHolder(DateTime time, WeightedAggregated weightedAggregatedValue) : this(time)
        {
            WeightedAggregatedValue = weightedAggregatedValue;
        }

        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public DataValueHolder(
            long timestamp,
            double? numberValue,
            object objectValue,
            string stringValue,
            Aggregated aggregatedValue,
            WeightedAggregated weightedAggregatedValue)
        {
            Timestamp = timestamp;
            NumberValue = numberValue;
            ObjectValue = objectValue;
            StringValue = stringValue;
            AggregatedValue = aggregatedValue;
            WeightedAggregatedValue = weightedAggregatedValue;
        }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Aggregated AggregatedValue { get; }

        [JsonIgnore]
        public DateTime DateTime
        {
            get => new DateTime(Timestamp);
        }

        public bool IsEmpty
        {
            get => NumberValue == null &&
                   ObjectValue == null &&
                   AggregatedValue == null &&
                   WeightedAggregatedValue == null &&
                   StringValue == null;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? NumberValue { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object ObjectValue { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string StringValue { get; }

        public long Timestamp { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public WeightedAggregated WeightedAggregatedValue { get; }

        /// <inheritdoc />
        public bool Equals(DataValueHolder other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return AggregatedValue == other.AggregatedValue &&
                   NumberValue.IsDoubleEqual(other.NumberValue) &&
                   ObjectValue == other.ObjectValue &&
                   StringValue.Equals(other.StringValue, StringComparison.CurrentCulture) &&
                   Timestamp == other.Timestamp &&
                   WeightedAggregatedValue == other.WeightedAggregatedValue;
        }

        public static DataValueHolder FromValue(DateTime time, object value, DataValueType valueType)
        {
            return GetWrapperFunction(valueType)
                ?.Invoke(new Tuple<DateTime, object>(time, valueType));
        }

        public static DataValueHolder FromValue<T>(DateTime time, T value)
        {
            var valueType = typeof(T).GetDataValueType();

            return GetWrapperFunction(valueType)
                ?.Invoke(new Tuple<DateTime, object>(time, valueType));
        }

        public static IEnumerable<DataValueHolder> FromValues<T>(
            IEnumerable<Tuple<DateTime, T>> values)
        {
            var valueType = typeof(T).GetDataValueType();

            return FromValues(values.Select(t => new Tuple<DateTime, object>(t.Item1, t.Item2)), valueType);
        }

        public static IEnumerable<DataValueHolder> FromValues(
            IEnumerable<Tuple<DateTime, object>> values,
            DataValueType valueType)
        {
            var wrapperFunction = GetWrapperFunction(valueType);

            if (wrapperFunction == null)
            {
                return null;
            }

            return values.Select(wrapperFunction);
        }

        public static bool operator ==(DataValueHolder left, DataValueHolder right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static explicit operator Tuple<DateTime, double?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, double?>(holder.DateTime, holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, double>(DataValueHolder holder)
        {
            return new Tuple<DateTime, double>(holder.DateTime, holder.NumberValue ?? 0D);
        }

        public static explicit operator Tuple<DateTime, long?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, long?>(holder.DateTime, (long?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, long>(DataValueHolder holder)
        {
            return new Tuple<DateTime, long>(holder.DateTime, (long?) holder.NumberValue ?? 0L);
        }

        public static explicit operator Tuple<DateTime, ulong?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, ulong?>(holder.DateTime, (ulong?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, ulong>(DataValueHolder holder)
        {
            return new Tuple<DateTime, ulong>(holder.DateTime, (ulong?) holder.NumberValue ?? 0UL);
        }

        public static explicit operator Tuple<DateTime, uint?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, uint?>(holder.DateTime, (uint?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, uint>(DataValueHolder holder)
        {
            return new Tuple<DateTime, uint>(holder.DateTime, (uint?) holder.NumberValue ?? 0U);
        }

        public static explicit operator Tuple<DateTime, int?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, int?>(holder.DateTime, (int?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, int>(DataValueHolder holder)
        {
            return new Tuple<DateTime, int>(holder.DateTime, (int?) holder.NumberValue ?? 0);
        }

        public static explicit operator Tuple<DateTime, ushort?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, ushort?>(holder.DateTime, (ushort?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, ushort>(DataValueHolder holder)
        {
            return new Tuple<DateTime, ushort>(holder.DateTime, (ushort?) holder.NumberValue ?? 0);
        }

        public static explicit operator Tuple<DateTime, short?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, short?>(holder.DateTime, (short?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, short>(DataValueHolder holder)
        {
            return new Tuple<DateTime, short>(holder.DateTime, (short?) holder.NumberValue ?? 0);
        }

        public static explicit operator Tuple<DateTime, byte?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, byte?>(holder.DateTime, (byte?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, byte>(DataValueHolder holder)
        {
            return new Tuple<DateTime, byte>(holder.DateTime, (byte?) holder.NumberValue ?? 0);
        }

        public static explicit operator Tuple<DateTime, float?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, float?>(holder.DateTime, (float?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, float>(DataValueHolder holder)
        {
            return new Tuple<DateTime, float>(holder.DateTime, (float?) holder.NumberValue ?? 0F);
        }

        public static explicit operator Tuple<DateTime, decimal?>(DataValueHolder holder)
        {
            return new Tuple<DateTime, decimal?>(holder.DateTime, (decimal?) holder.NumberValue);
        }

        public static explicit operator Tuple<DateTime, decimal>(DataValueHolder holder)
        {
            return new Tuple<DateTime, decimal>(holder.DateTime, (decimal?) holder.NumberValue ?? 0M);
        }

        public static explicit operator Tuple<DateTime, string>(DataValueHolder holder)
        {
            return new Tuple<DateTime, string>(holder.DateTime, holder.StringValue);
        }

        public static explicit operator Tuple<DateTime, WeightedAggregated>(DataValueHolder holder)
        {
            return new Tuple<DateTime, WeightedAggregated>(holder.DateTime, holder.WeightedAggregatedValue);
        }

        public static explicit operator Tuple<DateTime, Aggregated>(DataValueHolder holder)
        {
            return new Tuple<DateTime, Aggregated>(holder.DateTime,
                holder.WeightedAggregatedValue ?? holder.AggregatedValue);
        }

        public static explicit operator Tuple<DateTime, object>(DataValueHolder holder)
        {
            return new Tuple<DateTime, object>(
                holder.DateTime,
                holder.ObjectValue ??
                holder.WeightedAggregatedValue ??
                holder.AggregatedValue ??
                (object) holder.StringValue ??
                holder.NumberValue);
        }

        public static bool operator !=(DataValueHolder left, DataValueHolder right)
        {
            return !(left == right);
        }

        public static IEnumerable<Tuple<DateTime, T>> ToValues<T>(
            IEnumerable<DataValueHolder> holders)
        {
            var valueType = typeof(T).GetDataValueType();

            return ToValues(holders, valueType)?.Select(t => new Tuple<DateTime, T>(t.Item1, (T) t.Item2));
        }

        public static IEnumerable<Tuple<DateTime, object>> ToValues(
            IEnumerable<DataValueHolder> holders,
            DataValueType valueType)
        {
            var unWrapperFunction = GetUnWrapperFunction(valueType);

            if (unWrapperFunction == null)
            {
                return null;
            }

            return holders.Select(unWrapperFunction);
        }

        // ReSharper disable once TooManyDeclarations
        private static Func<DataValueHolder, Tuple<DateTime, object>> GetUnWrapperFunction(DataValueType valueType)
        {
            switch (valueType)
            {
                case DataValueType.Number:

                    return dto =>
                    {
                        if (dto.NumberValue == null)
                        {
                            return new Tuple<DateTime, object>(dto.DateTime, null);
                        }

                        return new Tuple<DateTime, object>(dto.DateTime, dto.NumberValue);
                    };

                case DataValueType.String:

                    return dto => new Tuple<DateTime, object>(dto.DateTime, dto.StringValue);

                case DataValueType.WeightedAggregated:

                    return dto => new Tuple<DateTime, object>(dto.DateTime, dto.WeightedAggregatedValue);
                case DataValueType.Aggregated:

                    return dto => new Tuple<DateTime, object>(dto.DateTime, dto.AggregatedValue);
                default:

                    return dto => new Tuple<DateTime, object>(dto.DateTime, dto.ObjectValue);
            }
        }

        // ReSharper disable once TooManyDeclarations
        private static Func<Tuple<DateTime, object>, DataValueHolder> GetWrapperFunction(
            DataValueType valueType)
        {
            switch (valueType)
            {
                case DataValueType.Number:

                    return t => new DataValueHolder(t.Item1,
                        t.Item2 != null ? (double?) Convert.ToDouble(t.Item2) : null);
                case DataValueType.String:

                    return t => new DataValueHolder(t.Item1, t.Item2.ToString());
                case DataValueType.WeightedAggregated:

                    return t =>
                        new DataValueHolder(t.Item1, t.Item2 as WeightedAggregated);
                case DataValueType.Aggregated:

                    return t => new DataValueHolder(t.Item1, t.Item2 as Aggregated);
                default:

                    return t => new DataValueHolder(t.Item1, t.Item2);
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DataValueHolder);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AggregatedValue != null ? AggregatedValue.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ NumberValue.GetHashCode();
                hashCode = (hashCode * 397) ^ (ObjectValue != null ? ObjectValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringValue != null ? StringValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^
                           (WeightedAggregatedValue != null ? WeightedAggregatedValue.GetHashCode() : 0);

                return hashCode;
            }
        }

        public DataValueHolder Clone(DateTime newDateTime)
        {
            return new DataValueHolder(newDateTime.Ticks, NumberValue, ObjectValue, StringValue, AggregatedValue,
                WeightedAggregatedValue);
        }

        public Tuple<DateTime, T> ToValue<T>()
        {
            var unwrappedValue = GetUnWrapperFunction(typeof(T).GetDataValueType())?.Invoke(this);

            if (unwrappedValue == null)
            {
                return null;
            }

            return new Tuple<DateTime, T>(unwrappedValue.Item1, (T) unwrappedValue.Item2);
        }

        public Tuple<DateTime, object> ToValue(DataValueType valueType)
        {
            return GetUnWrapperFunction(valueType)?.Invoke(this);
        }
    }
}