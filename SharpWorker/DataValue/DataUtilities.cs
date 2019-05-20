using System;
using System.Collections.Generic;
using System.Linq;
using SharpWorker.DataStore.WebApiControllers.DTOModels;

namespace SharpWorker.DataValue
{
    public static class DataUtilities
    {
        public static DataValueType GetDataValueType(this Type type)
        {
            if (type == typeof(double?) ||
                type == typeof(double) ||
                type == typeof(decimal?) ||
                type == typeof(decimal) ||
                type == typeof(float?) ||
                type == typeof(float) ||
                type == typeof(long?) ||
                type == typeof(long) ||
                type == typeof(ulong?) ||
                type == typeof(ulong) ||
                type == typeof(int?) ||
                type == typeof(int) ||
                type == typeof(uint?) ||
                type == typeof(uint) ||
                type == typeof(short?) ||
                type == typeof(short) ||
                type == typeof(ushort?) ||
                type == typeof(ushort) ||
                type == typeof(byte?) ||
                type == typeof(byte))
            {
                return DataValueType.Number;
            }

            if (type.IsValueType ||
                type == typeof(string))
            {
                return DataValueType.String;
            }

            if (typeof(WeightedAggregated).IsAssignableFrom(type))
            {
                return DataValueType.WeightedAggregated;
            }

            if (typeof(Aggregated).IsAssignableFrom(type))
            {
                return DataValueType.Aggregated;
            }

            return DataValueType.Object;
        }

        public static double? GuessTimeFrameStat(
            this IEnumerable<Tuple<DateTime, double>> points,
            DateTime start,
            DateTime end)
        {
            var orderedPoints = points?.OrderBy(t => t.Item1).ToArray();
            var before = orderedPoints?.Where(t => t.Item1 < start).LastOrDefault();
            var after = orderedPoints?.Where(t => t.Item1 > end).FirstOrDefault();
            var last = orderedPoints?.Where(t => t.Item1 <= end).LastOrDefault();
            var first = orderedPoints?.Where(t => t.Item1 >= start).FirstOrDefault();

            double stat;
            TimeSpan statDuration;

            if (last != null && first != null)
            {
                // has value inside

                // calculate center real value from inside values
                stat = last.Item2 - first.Item2;
                statDuration = last.Item1 - first.Item1;

                if (before != null)
                {
                    // predict first part from before value
                    var durationToFirst = first.Item1 - start;
                    stat += (first.Item2 - before.Item2) *
                            ((double) durationToFirst.Ticks / (first.Item1 - before.Item1).Ticks);
                    statDuration += durationToFirst;
                }

                if (after != null)
                {
                    // predict last part from after value
                    var durationFromLast = end - last.Item1;
                    stat += (after.Item2 - last.Item2) *
                            ((double) durationFromLast.Ticks / (after.Item1 - last.Item1).Ticks);
                    statDuration += durationFromLast;
                }
            }
            else if (after != null && before != null)
            {
                // has no value inside, but an after and a before value are available
                statDuration = after.Item1 - before.Item1;
                stat = after.Item2 - before.Item2;
            }
            else
            {
                // has no value inside and there is no after or before value available
                // ignore
                return null;
            }

            if (statDuration.Ticks > 0)
            {
                return stat * ((double) (end - start).Ticks / statDuration.Ticks);
            }

            return null;
        }

        public static bool IsDoubleEqual(this double? a, double? b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            return Math.Abs(a.Value - b.Value) <= Math.Max(Math.Abs(a.Value), Math.Abs(b.Value)) * 1E-15;
        }

        public static IEnumerable<double> ToChanges(this IEnumerable<double> absoluteValues, double startValue)
        {
            foreach (var absoluteValue in absoluteValues)
            {
                yield return absoluteValue - startValue;
                startValue = absoluteValue;
            }
        }

        public static DateTime ToDateFractured(this DateTime date, TimeSpan duration)
        {
            var durationInTicks = duration.Ticks;

            if (durationInTicks == 0)
            {
                return date;
            }

            return new DateTime((long) (Math.Floor((double) date.Ticks / durationInTicks) * durationInTicks),
                date.Kind);
        }
    }
}