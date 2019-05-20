using System;
using System.Collections.Generic;
using System.Linq;
using SharpWorker.DataStore.WebApiControllers.DTOModels;

namespace SharpWorker.DataValue
{
    public static class DataGranularizerUtilities
    {
        // ReSharper disable once TooManyArguments
        public static DataValueHolder[] GranularizeValues(
            this IEnumerable<DataValueHolder> values,
            DataValueType valueType,
            TimeSpan granularityDuration,
            // ReSharper disable once FlagArgument
            DataGranularityMode granularityMode,
            DataGranularityFillMode dataGranularityFillMode,
            int maxCount = int.MaxValue)
        {
            if (granularityMode == DataGranularityMode.None)
            {
                return values.Take(maxCount).ToArray();
            }

            var granularizerFunction = GetGranularizerFunction(granularityMode, valueType);
            var granularizerFillFunction = GetGranularizerFillFunction(dataGranularityFillMode, valueType);

            if (granularizerFunction == null)
            {
                return values.Take(maxCount).ToArray();
            }

            var granularizedValues = GranularizeValues(values, granularityDuration, granularizerFunction).Take(maxCount).ToArray();

            if (granularizerFillFunction != null)
            {
                for (var i = 0; i < granularizedValues.Length; i++)
                {
                    if (granularizedValues[i].IsEmpty)
                    {
                        // ReSharper disable once EventExceptionNotDocumented
                        granularizedValues[i] = granularizerFillFunction(granularizedValues, i);
                    }
                }
            }

            return granularizedValues;
        }

        // ReSharper disable once TooManyDeclarations
        private static DataValueHolder AggregateAggregatedGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            var open = last?.AggregatedValue?.Close ??
                       values.FirstOrDefault(value => value?.AggregatedValue?.Open != null)
                           ?.AggregatedValue?.Open;

            var close = values.LastOrDefault(value => value?.AggregatedValue?.Close != null)
                            ?.AggregatedValue?.Close ??
                        open;

            var high = values.Any(value => value?.AggregatedValue?.High != null)
                ? values.Where(value => value?.AggregatedValue?.High != null)
                    .Max(value => value.AggregatedValue.High)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                : (open != null && close != null
                    ? Math.Max(open.Value, close.Value)
                    // ReSharper disable once ConstantNullCoalescingCondition
                    : (open ?? close));

            var low = values.Any(value => value?.AggregatedValue?.Low != null)
                ? values.Where(value => value?.AggregatedValue?.Low != null)
                    .Min(value => value.AggregatedValue.Low)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                : (open != null && close != null
                    ? Math.Min(open.Value, close.Value)
                    // ReSharper disable once ConstantNullCoalescingCondition
                    : (open ?? close));

            return new DataValueHolder(time, new Aggregated(open, high, low, close));
        }

        private static DataValueHolder AggregateNumberGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            if (last?.AggregatedValue?.Close != null)
            {
                return new DataValueHolder(time,
                    new Aggregated(values.Select(value => value.NumberValue ?? 0).ToArray(),
                        last.AggregatedValue.Close));
            }

            return new DataValueHolder(time, new Aggregated(values.Select(value => value.NumberValue ?? 0).ToArray()));
        }

        // ReSharper disable once TooManyDeclarations
        private static DataValueHolder AggregateWeightedAggregatedGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            var open = last?.WeightedAggregatedValue?.Close ??
                       values.FirstOrDefault(value => value?.WeightedAggregatedValue?.Open != null)
                           ?.WeightedAggregatedValue?.Open;

            var close = values.LastOrDefault(value => value?.WeightedAggregatedValue?.Close != null)
                            ?.WeightedAggregatedValue?.Close ??
                        open;

            var high = values.Any(value => value?.WeightedAggregatedValue?.High != null)
                ? values.Where(value => value?.WeightedAggregatedValue?.High != null)
                    .Max(value => value.WeightedAggregatedValue.High)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                : (open != null && close != null
                    ? Math.Max(open.Value, close.Value)
                    // ReSharper disable once ConstantNullCoalescingCondition
                    : (open ?? close));

            var low = values.Any(value => value?.WeightedAggregatedValue?.Low != null)
                ? values.Where(value => value?.WeightedAggregatedValue?.Low != null)
                    .Min(value => value.WeightedAggregatedValue.Low)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                : (open != null && close != null
                    ? Math.Min(open.Value, close.Value)
                    // ReSharper disable once ConstantNullCoalescingCondition
                    : (open ?? close));

            var totalWeight = values.Select(value => value?.WeightedAggregatedValue?.TotalWeight)
                .Where(value => value != null)
                .DefaultIfEmpty()
                .Sum();

            var weightedValue = totalWeight > 0 || totalWeight < 0
                ? values.Sum(value => (value.WeightedAggregatedValue.TotalWeight ?? 0) *
                                      (value.WeightedAggregatedValue.WeightedValue ?? 0)) /
                  totalWeight
                : null;

            return new DataValueHolder(time,
                new WeightedAggregated(open, high, low, close, weightedValue, totalWeight));
        }

        private static DataValueHolder AverageNumberGranularizerFillFunction(DataValueHolder[] values, int i)
        {
            var previousIndex = GetPreviousIndex(values, i);
            var nextIndex = GetNextIndex(values, i);
            var change = nextIndex - previousIndex;

            if (change > 0 &&
                previousIndex >= 0 &&
                nextIndex < values.Length &&
                !values[previousIndex].IsEmpty &&
                !values[nextIndex].IsEmpty)
            {
                var difference = (values[nextIndex].NumberValue - values[previousIndex].NumberValue) /
                                 change;

                for (var j = 0; j < change - 1; j++)
                {
                    values[j + i] = new DataValueHolder(values[j + i].DateTime,
                        values[previousIndex].NumberValue + difference * (j + 1));
                }
            }

            return values[i];
        }

        private static DataValueHolder AverageNumberGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            return new DataValueHolder(time, values.Average(value => value.NumberValue));
        }

        private static DataValueHolder FirstRecordGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            return values.First().Clone(time);
        }

        // ReSharper disable once ExcessiveIndentation
        // ReSharper disable once MethodTooLong
        private static GranularizerFillFunction GetGranularizerFillFunction(
            DataGranularityFillMode fillMode,
            DataValueType valueType)
        {
            switch (fillMode)
            {
                case DataGranularityFillMode.Average:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return AverageNumberGranularizerFillFunction;
                        default:

                            return null;
                    }
                case DataGranularityFillMode.Zero:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return ZeroNumberGranularizerFillFunction;
                        default:

                            return null;
                    }
                case DataGranularityFillMode.Previous:

                    return PreviousValueGranularizerFillFunction;
                case DataGranularityFillMode.Next:

                    return NextValueGranularizerFillFunction;
                default:

                    return (values, i) => values[i];
            }
        }

        private static GranularizerFunction GetGranularizerFunction(
            DataGranularityMode granularityMode,
            DataValueType valueType)
        {
            switch (granularityMode)
            {
                case DataGranularityMode.Last:

                    return LastRecordGranularizerFunction;
                case DataGranularityMode.First:

                    return FirstRecordGranularizerFunction;
                case DataGranularityMode.Average:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return AverageNumberGranularizerFunction;
                        default:

                            return null;
                    }
                case DataGranularityMode.Max:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return MaxNumberGranularizerFunction;
                        default:

                            return null;
                    }
                case DataGranularityMode.Min:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return MinNumberGranularizerFunction;
                        default:

                            return null;
                    }
                case DataGranularityMode.Sum:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return SumNumberGranularizerFunction;
                        default:

                            return null;
                    }
                case DataGranularityMode.Aggregated:

                    switch (valueType)
                    {
                        case DataValueType.Number:

                            return AggregateNumberGranularizerFunction;
                        case DataValueType.Aggregated:

                            return AggregateAggregatedGranularizerFunction;
                        case DataValueType.WeightedAggregated:

                            return AggregateWeightedAggregatedGranularizerFunction;
                        default:

                            return null;
                    }
                default:

                    return null;
            }
        }

        private static int GetNextIndex(DataValueHolder[] values, int index)
        {
            while (index < values.Length - 1 && values[index + 1].IsEmpty)
            {
                index++;
            }

            return index + 1;
        }

        private static int GetPreviousIndex(DataValueHolder[] values, int index)
        {
            while (index > 0 && values[index - 1].IsEmpty)
            {
                index--;
            }

            return index - 1;
        }

        private static IEnumerable<DataValueHolder> GranularizeValues(
            IEnumerable<DataValueHolder> values,
            TimeSpan granularityDuration,
            GranularizerFunction granularizerFunction)
        {
            DateTime? recordStartDate = null;
            DateTime? recordEndDate = null;
            var currentRecords = new List<DataValueHolder>();
            DataValueHolder lastAttributeValue = null;

            foreach (var dataRecord in values)
            {
                if (granularizerFunction == null || granularityDuration == TimeSpan.Zero)
                {
                    yield return dataRecord;
                }

                if (recordStartDate == null)
                {
                    recordStartDate = dataRecord.DateTime;
                    recordEndDate = recordStartDate + granularityDuration;
                }

                while (recordEndDate.Value < dataRecord.DateTime)
                {
                    if (currentRecords.Any())
                    {
                        // End of duration
                        // ReSharper disable once PossibleNullReferenceException
                        // ReSharper disable once EventExceptionNotDocumented
                        var retVal = granularizerFunction(
                            currentRecords.ToArray(),
                            lastAttributeValue,
                            recordStartDate.Value
                        );
                        currentRecords.Clear();
                        lastAttributeValue = retVal;

                        yield return retVal;
                    }
                    else
                    {
                        // Empty record
                        yield return new DataValueHolder(recordStartDate.Value);
                    }

                    recordStartDate = recordEndDate;
                    recordEndDate = recordStartDate + granularityDuration;
                }

                if (recordStartDate.Value <= dataRecord.DateTime)
                {
                    // Add to the current duration record list
                    currentRecords.Add(dataRecord);
                }
            }

            if (currentRecords.Any() && recordStartDate != null)
            {
                // end of duration
                // ReSharper disable once PossibleNullReferenceException
                // ReSharper disable once EventExceptionNotDocumented
                yield return granularizerFunction(currentRecords.ToArray(), lastAttributeValue, recordStartDate.Value);
            }
        }


        private static DataValueHolder LastRecordGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            return values.Last().Clone(time);
        }

        private static DataValueHolder MaxNumberGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            return new DataValueHolder(time, values.Max(value => value.NumberValue));
        }

        private static DataValueHolder MinNumberGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            return new DataValueHolder(time, values.Min(value => value.NumberValue));
        }

        private static DataValueHolder NextValueGranularizerFillFunction(DataValueHolder[] values, int i)
        {
            var nextIndex = GetNextIndex(values, i);

            if (nextIndex < values.Length)
            {
                values[i] = values[nextIndex].Clone(values[i].DateTime);
            }

            return values[i];
        }

        private static DataValueHolder PreviousValueGranularizerFillFunction(DataValueHolder[] values, int i)
        {
            var previousIndex = GetPreviousIndex(values, i);

            if (previousIndex >= 0)
            {
                values[i] = values[previousIndex].Clone(values[i].DateTime);
            }

            return values[i];
        }

        private static DataValueHolder SumNumberGranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder last,
            DateTime time)
        {
            return new DataValueHolder(time, values.Sum(value => value.NumberValue));
        }

        private static DataValueHolder ZeroNumberGranularizerFillFunction(DataValueHolder[] values, int i)
        {
            values[i] = new DataValueHolder(values[i].DateTime, 0d);

            return values[i];
        }

        private delegate DataValueHolder GranularizerFillFunction(DataValueHolder[] values, int index);

        private delegate DataValueHolder GranularizerFunction(
            DataValueHolder[] values,
            DataValueHolder lastGranularizedValue,
            DateTime dateTime);
    }
}