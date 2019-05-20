using System;
using System.Linq;
using Newtonsoft.Json;

namespace SharpWorker.DataValue
{
    public class Aggregated : IEquatable<Aggregated>
    {
        public Aggregated()
        {
        }

        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public Aggregated(double? open, double? high, double? low, double? close)
        {
            Open = open;
            Close = close;
            High = high;
            Low = low;
        }

        public Aggregated(double[] points, double? open) : this(points)
        {
            Open = open;
        }

        public Aggregated(double[] points)
        {
            Open = points.FirstOrDefault();
            Close = points.LastOrDefault();

            if (points.Any())
            {
                High = points.Max();
                Low = points.Min();
            }
        }

        public double? Close { get; protected set; }
        public double? High { get; protected set; }
        public double? Low { get; protected set; }
        public double? Open { get; protected set; }

        /// <inheritdoc />
        public bool Equals(Aggregated other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Close.IsDoubleEqual(other.Close) &&
                   High.IsDoubleEqual(other.High) &&
                   Low.IsDoubleEqual(other.Low) &&
                   Open.IsDoubleEqual(other.Open);
        }

        public static bool operator ==(Aggregated left, Aggregated right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator >(Aggregated left, double? right)
        {
            return right > left.High;
        }

        public static bool operator >=(Aggregated left, double? right)
        {
            return right >= left.High;
        }

        public static bool operator !=(Aggregated left, Aggregated right)
        {
            return !(left == right);
        }

        public static bool operator <(Aggregated left, double? right)
        {
            return right < left.Low;
        }

        public static bool operator <=(Aggregated left, double? right)
        {
            return right <= left.Low;
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

            return Equals(obj as Aggregated);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = Close.GetHashCode();
                hashCode = (hashCode * 397) ^ High.GetHashCode();
                hashCode = (hashCode * 397) ^ Low.GetHashCode();
                hashCode = (hashCode * 397) ^ Open.GetHashCode();
                // ReSharper restore NonReadonlyMemberInGetHashCode

                return hashCode;
            }
        }
    }
}