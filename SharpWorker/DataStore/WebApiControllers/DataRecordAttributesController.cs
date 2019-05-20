using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using SharpWorker.DataStore.Query;
using SharpWorker.DataStore.WebApiControllers.DTOModels;
using SharpWorker.DataValue;
using SharpWorker.Log;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataStore.WebApiControllers
{
    // ReSharper disable once HollowTypeName
    [WebApiController("data")]
    public class DataRecordAttributesController : WebApiController
    {
        private readonly DataStoreBase _dataStore;
        private readonly Logger _logger;

        public DataRecordAttributesController(DataStoreBase dataStore, Logger logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        // ReSharper disable once TooManyArguments
        // ReSharper disable once TooManyDeclarations
        private static RecordAttributeResponseDTO GetAttributeValues(
            DataTopic topic,
            string attributeName,
            IEnumerable<DataRecord> records,
            RecordAttributeGranularityRequestDTO granularityRequest)
        {
            var type = topic?.GetProviderType();
            var property = type?.GetProperty(attributeName);

            if (property == null)
            {
                throw new InvalidOperationException("Bad property name or topic.");
            }

            var attributeType = property.PropertyType.GetDataValueType();
            var granularityDuration = new TimeSpan(granularityRequest.GranularityDuration ?? 0);

            var attributeRecords = DataValueHolder.FromValues(records.Where(record => record.CreatedDateTime != null)
                .Select(record =>
                    new Tuple<DateTime, object>(record.CreatedDateTime.Value.ToDateFractured(granularityDuration),
                        property.GetValue(record))), attributeType);

            if (attributeRecords == null)
            {
                throw new InvalidOperationException("Value wrapping operation failed.");
            }


            var granularifiedAttributeRecords = attributeRecords.GranularizeValues(
                attributeType,
                granularityDuration,
                granularityRequest.GranularityMode,
                granularityRequest.GranularityFill,
                DataRecordsController.MaximumRecords + 2
            );

            return new RecordAttributeResponseDTO
            {
                Count = granularifiedAttributeRecords.Length - 1,
                Start = granularifiedAttributeRecords.Length > 1
                    ? (long?) granularifiedAttributeRecords.Skip(1).Min(record => record.Timestamp)
                    : null,
                End = granularifiedAttributeRecords.Length > 1
                    ? (long?) granularifiedAttributeRecords.Skip(1).Max(record => record.Timestamp)
                    : null,
                Values = granularifiedAttributeRecords.Skip(1)
            };
        }


        [WebApiRequest(WebApiRequestMethod.Get, "{provider}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(ProviderAttributeResponseDTO[]))]
        // ReSharper disable once TooManyDeclarations
        public WebApiResponse GetAttributes(
            [Description("The name of the provider.")]
            string provider)
        {
            try
            {
                var type = _dataStore.GetTopics(provider).FirstOrDefault()?.GetProviderType();

                if (type == null)
                {
                    return NotFound();
                }

                var attributes = type.GetProperties().Where(info =>
                        info.GetMethod != null && info.CanRead && info.DeclaringType != typeof(DataRecord)).Select(
                        info =>
                            new ProviderAttributeResponseDTO(info.Name, info.PropertyType.GetDataValueType()))
                    .OrderBy(dto => dto.AttributeType).ToArray();

                return Ok(attributes);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordAttributesController), nameof(GetAttributes), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(RecordAttributeResponseDTO))]
        [WebApiRequest(WebApiRequestMethod.Get, "{provider}/{attributeName}/{topicId}/{startTimestamp}/{endTimestamp}", "read")]
        // ReSharper disable once MethodTooLong
        // ReSharper disable once TooManyArguments
        public WebApiResponse GetAttributeValues(
            string provider,
            string attributeName,
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [Description("Creation date start point.")]
            long startTimestamp,
            [Description("Creation date end point.")]
            long endTimestamp,
            [WebApiFromBody] RecordAttributeGranularityRequestDTO granularityRequest = null)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                var granularityDuration = new TimeSpan(granularityRequest?.GranularityDuration ?? 0);
                var startDate = new DateTime(startTimestamp).ToDateFractured(granularityDuration)
                    .Add(-granularityDuration);
                var endDate = new DateTime(endTimestamp).ToDateFractured(granularityDuration).Add(granularityDuration);
                var records = DataRecordsController.GetCreatedRecords(_dataStore, topic, startDate, endDate,
                    granularityRequest?.Condition);
                var values = GetAttributeValues(topic, attributeName, records, granularityRequest);

                if (values == null)
                {
                    return NotFound();
                }

                return Ok(values);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), nameof(GetAttributeValues), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(RecordAttributeResponseDTO))]
        [WebApiRequest(WebApiRequestMethod.Get, "{provider}/{attributeName}/{topicId}/{startTimestamp}", "read")]
        // ReSharper disable once TooManyArguments
        public WebApiResponse GetAttributeValues(
            string provider,
            string attributeName,
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [Description("Creation date start point.")]
            long startTimestamp,
            [WebApiFromBody] RecordAttributeGranularityRequestDTO granularityRequest = null)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                var granularityDuration = new TimeSpan(granularityRequest?.GranularityDuration ?? 0);
                var startDate = new DateTime(startTimestamp).ToDateFractured(granularityDuration)
                    .Add(-granularityDuration);
                var records = DataRecordsController.GetCreatedRecords(_dataStore, topic, startDate, null,
                    granularityRequest?.Condition);
                var values = GetAttributeValues(topic, attributeName, records, granularityRequest);

                if (values == null)
                {
                    return NotFound();
                }

                return Ok(values);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), nameof(GetAttributeValues), LogType.Error, e);

                return InternalServerError(e);
            }
        }

        [WebApiResponse(HttpStatusCode.OK, typeof(RecordAttributeResponseDTO))]
        [WebApiRequest(WebApiRequestMethod.Get, "{provider}/{attributeName}/{topicId}", "read")]
        // ReSharper disable once TooManyArguments
        public WebApiResponse GetAttributeValues(
            string provider,
            string attributeName,
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [WebApiFromBody] RecordAttributeGranularityRequestDTO granularityRequest)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                var records = DataRecordsController.GetRawRecords(_dataStore, topic, granularityRequest?.Condition,
                    new DataStoreSortQuery(new DataStoreQueryExpressionIndex(nameof(DataRecord.CreatedTimestamp)),
                        DataStoreSortQueryDirection.Ascending));
                var values = GetAttributeValues(topic, attributeName, records, granularityRequest);

                if (values == null)
                {
                    return NotFound();
                }

                return Ok(values);
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), nameof(GetAttributeValues), LogType.Error, e);

                return InternalServerError(e);
            }
        }
    }
}