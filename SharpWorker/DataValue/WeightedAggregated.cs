using System;
using System.Linq;
using Newtonsoft.Json;

namespace SharpWorker.DataValue
{
    public class WeightedAggregated : Aggregated, IEquatable<WeightedAggregated>
    {
        public WeightedAggregated()
        {
        }

        // ReSharper disable once TooManyDependencies
        [JsonConstructor]
        public WeightedAggregated(
            double? open,
            double? high,
            double? low,
            double? close,
            double? weightedValue = null,
            double? totalWeight = null) : base(open, high, low, close)
        {
            WeightedValue = weightedValue;
            TotalWeight = totalWeight;
        }

        public WeightedAggregated(Tuple<double, double>[] weightedPoints, double open) : this(weightedPoints)
        {
            Open = open;
        }

        public WeightedAggregated(Tuple<double, double>[] weightedPoints) : base(weightedPoints.Select(t => t.Item1)
            .ToArray())
        {
            var sum = weightedPoints.DefaultIfEmpty().Sum(t => t.Item2);

            if (sum > 1E-15)
            {
                TotalWeight = sum;
                WeightedValue = weightedPoints.Sum(t => t.Item1 * t.Item2) / sum;
            }
        }

        public WeightedAggregated(double[] points, double? weight) : base(points)
        {
            TotalWeight = weight;
        }

        public WeightedAggregated(double[] points, double? open, double? weight) : base(points, open)
        {
            TotalWeight = weight;
        }

        // ReSharper disable once TooManyDependencies
        public WeightedAggregated(double[] points, double? open, double? weight, double? weightedValue) : this(points,
            open, weight)
        {
            WeightedValue = weightedValue;
        }

        public double? TotalWeight { get; protected set; }

        public double? WeightedValue { get; protected set; }

        /// <inheritdoc />
        public bool Equals(WeightedAggregated other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) &&
                   TotalWeight.IsDoubleEqual(other.TotalWeight) &&
                   WeightedValue.IsDoubleEqual(other.WeightedValue);
        }

        public static bool operator ==(WeightedAggregated left, WeightedAggregated right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(WeightedAggregated left, WeightedAggregated right)
        {
            return !(left == right);
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

            return Equals(obj as WeightedAggregated);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                // ReSharper disable NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ TotalWeight.GetHashCode();
                hashCode = (hashCode * 397) ^ WeightedValue.GetHashCode();
                // ReSharper restore NonReadonlyMemberInGetHashCode

                return hashCode;
            }
        }
    }
}