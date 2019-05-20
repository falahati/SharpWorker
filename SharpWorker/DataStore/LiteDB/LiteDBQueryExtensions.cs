using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using SharpWorker.DataStore.Query;

namespace SharpWorker.DataStore.LiteDB
{
    internal static class LiteDBQueryExtensions
    {
        public static string GetName(this IDataStoreQueryVariableValue queryVariableValue)
        {
            if (queryVariableValue is DataStoreQueryField queryField)
            {
                if (queryField.FieldName.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "_id";
                }

                return queryField.FieldName;
            }

            if (queryVariableValue is DataStoreQueryExpressionIndex queryIndex)
            {
                return GetIndexName(queryIndex.IndexName);
            }

            throw new NotSupportedException("IDataStoreQueryConstant type is not supported.");
        }

        public static string GetIndexName(string indexName)
        {
            if (indexName.StartsWith("_"))
            {
                return indexName;
            }

            return "idx_" + indexName;
        }

        public static IEnumerable<T> SortBy<T>(this IEnumerable<T> records, DataStoreSortQuery sortQuery)
        {
            if (sortQuery.Value is DataStoreQueryField queryField)
            {
                var property = typeof(T).GetProperty(queryField.FieldName);

                if (property == null)
                {
                    return new T[0];
                }

                if (sortQuery.Direction == DataStoreSortQueryDirection.Descending)
                {
                    return records.OrderByDescending(arg => property.GetValue(arg));
                }

                return records.OrderBy(arg => property.GetValue(arg));
            }

            throw new NotSupportedException("DataStoreSortQuery.Value type is not supported.");
        }

        public static BsonValue[] ToBsonArray(this DataStoreQueryConstant queryConstant)
        {
            return (queryConstant.Value as IEnumerable)
                   ?.Cast<dynamic>()
                   .Select(o => (o == null ? null : InternalToBsonValue(o)) as BsonValue)
                   .ToArray() ??
                   new BsonValue[0];
        }

        public static BsonValue ToBsonValue(this DataStoreQueryConstant queryConstant)
        {
            return (queryConstant?.Value == null ? null : InternalToBsonValue(queryConstant.Value)) as BsonValue;
        }


        public static global::LiteDB.Query ToQuery(this DataStoreSortQuery sortQuery)
        {
            if (sortQuery.Value is IDataStoreQueryVariableValue queryValue)
            {
                var fieldOrIndexName = queryValue.GetName();
                var direction =
                    sortQuery.Direction == DataStoreSortQueryDirection.Ascending
                        ? global::LiteDB.Query.Ascending
                        : global::LiteDB.Query.Descending;

                return global::LiteDB.Query.All(fieldOrIndexName, direction);
            }

            throw new NotSupportedException("DataStoreSortQuery.Value type is not supported.");
        }

        public static global::LiteDB.Query ToQuery(this DataStoreCompareQuery compareQuery)
        {
            var bsonValue = compareQuery.Value.ToBsonValue();
            var fieldOrIndexName = compareQuery.FieldOrIndex.GetName();

            switch (compareQuery.Type)
            {
                case DataStoreCompareQueryType.Equal:

                    return global::LiteDB.Query.EQ(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.EqualOrGreater:

                    return global::LiteDB.Query.GTE(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.Greater:

                    return global::LiteDB.Query.GT(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.EqualOrLess:

                    return global::LiteDB.Query.LTE(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.Less:

                    return global::LiteDB.Query.LT(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.Contains:

                    return global::LiteDB.Query.Contains(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.StartsWith:

                    return global::LiteDB.Query.StartsWith(fieldOrIndexName, bsonValue);
                case DataStoreCompareQueryType.EndsWith:

                    return global::LiteDB.Query.Where(fieldOrIndexName,
                        value => value.AsString?.EndsWith(bsonValue) == true);
                case DataStoreCompareQueryType.In:

                    return global::LiteDB.Query.In(fieldOrIndexName, compareQuery.Value.ToBsonArray());
                case DataStoreCompareQueryType.NotEqual:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.EQ(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotEqualOrGreater:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.GTE(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotGreater:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.GT(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotEqualOrLess:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.LTE(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotLess:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.LT(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotContains:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.Contains(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotStartsWith:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.StartsWith(fieldOrIndexName, bsonValue));
                case DataStoreCompareQueryType.NotEndsWith:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.Where(fieldOrIndexName,
                        value => value.AsString?.EndsWith(bsonValue) == true));
                case DataStoreCompareQueryType.NotIn:

                    return global::LiteDB.Query.Not(global::LiteDB.Query.In(fieldOrIndexName,
                        compareQuery.Value.ToBsonArray()));
                default:

                    throw new ArgumentOutOfRangeException(nameof(compareQuery.Type));
            }
        }

        public static global::LiteDB.Query ToQuery(this DataStoreOperatorQuery operatorQuery)
        {
            var left = operatorQuery.Left.ToQuery();
            var right = operatorQuery.Right.ToQuery();

            switch (operatorQuery.Type)
            {
                case DataStoreOperatorQueryType.And:

                    return global::LiteDB.Query.And(left, right);
                case DataStoreOperatorQueryType.Or:

                    return global::LiteDB.Query.Or(left, right);
                default:

                    throw new ArgumentOutOfRangeException(nameof(operatorQuery.Type));
            }
        }

        public static global::LiteDB.Query ToQuery(this DataStoreQueryCalculatedValue queryCalculatedValue)
        {
            if (queryCalculatedValue is DataStoreCompareQuery compareQuery)
            {
                return compareQuery.ToQuery();
            }

            if (queryCalculatedValue is DataStoreOperatorQuery operatorQuery)
            {
                return operatorQuery.ToQuery();
            }

            throw new NotSupportedException("IDataStoreQueryOperation type is not supported.");
        }

        private static BsonValue InternalToBsonValue(dynamic value)
        {
            if (value is Enum)
            {
                var enumName = Enum.GetName(value.GetType(), value);

                return new BsonValue(enumName);
            }

            return new BsonValue(value);
        }
    }
}