using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using SharpWorker.DataStore.Query;
using SharpWorker.DataStore.WebApiControllers.DTOModels;
using SharpWorker.Log;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataStore.WebApiControllers
{
    [Description("Provides access to database records.")]
    [WebApiController("data")]
    // ReSharper disable once HollowTypeName
    public class DataRecordsController : WebApiController
    {
        internal const int MaximumRecords = 1000;
        private readonly DataStoreBase _dataStore;
        private readonly Logger _logger;

        public DataRecordsController(DataStoreBase dataStore, Logger logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        // ReSharper disable once TooManyArguments
        internal static IEnumerable<DataRecord> GetCreatedRecords(
            DataStoreBase dataStore,
            DataTopic topic,
            DateTime? start,
            DateTime? end,
            DataStoreQueryCalculatedValue condition)
        {
            return (typeof(DataStoreBase).GetMethod(nameof(DataStoreBase.GetCreated))
                    ?.MakeGenericMethod(topic.GetProviderType())
                    .Invoke(dataStore,
                        new object[]
                        {
                            topic,
                            start,
                            end,
                            condition,
                            0,
                            int.MaxValue
                        }) as IEnumerable)
                ?.Cast<DataRecord>();
        }

        // ReSharper disable once TooManyArguments
        internal static IEnumerable<DataRecord> GetRawRecords(
            DataStoreBase dataStore,
            DataTopic topic,
            DataStoreQueryCalculatedValue condition,
            DataStoreSortQuery sort)
        {
            return (typeof(DataStoreBase).GetMethod(nameof(DataStoreBase.GetRecords))
                    ?.MakeGenericMethod(topic.GetProviderType())
                    .Invoke(dataStore,
                        new object[]
                        {
                            topic,
                            condition,
                            sort,
                            0,
                            int.MaxValue
                        }) as IEnumerable)
                ?.Cast<DataRecord>();
        }

        [Description("Deletes records based on the condition provided.")]
        [WebApiRequest(WebApiRequestMethod.Delete, "{topicId}", "write")]
        [WebApiResponse(HttpStatusCode.OK, typeof(int))]
        // ReSharper disable once TooManyDeclarations
        public WebApiResponse DeleteRecords(
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [Description("The query filter and sort conditions.")] [WebApiFromBody]
            DataStoreQueryCalculatedValue condition)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                if (condition == null)
                {
                    return BadRequest();
                }

                return Ok(_dataStore.DeleteRecords(topic, condition));
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), LogType.Error, nameof(DeleteRecords), e);

                return InternalServerError(e);
            }
        }

        [Description("Gets records based on the condition provided.")]
        [WebApiRequest(WebApiRequestMethod.Get, "{topicId}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(RecordResponseDTO))]
        // ReSharper disable once TooManyDeclarations
        public WebApiResponse GetRecords(
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [Description("The query filter and sort conditions.")] [WebApiFromBody]
            RecordRequestDTO request = null)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                var records = GetRawRecords(_dataStore, topic, request?.Condition, request?.Sort).Take(MaximumRecords)
                    .ToArray();

                return Ok(new RecordResponseDTO
                {
                    Count = records.Length,
                    Start = records.Length > 0 ? records.Min(record => record.CreatedTimestamp) : null,
                    End = records.Length > 0 ? records.Max(record => record.CreatedTimestamp) : null,
                    Values = records
                });
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), nameof(GetRecords),LogType.Error, e);

                return InternalServerError(e);
            }
        }


        [Description("Gets records based on the creation date condition provided.")]
        [WebApiRequest(WebApiRequestMethod.Get, "{topicId}/{startTimestamp}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(RecordResponseDTO))]
        // ReSharper disable once TooManyDeclarations
        public WebApiResponse GetRecords(
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [Description("Creation date start point.")]
            long startTimestamp)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                var records = GetCreatedRecords(_dataStore, topic, new DateTime(startTimestamp), null, null)
                    .Take(MaximumRecords).ToArray();

                return Ok(new RecordResponseDTO
                {
                    Count = records.Length,
                    Start = records.Length > 0 ? records.Min(record => record.CreatedTimestamp) : null,
                    End = records.Length > 0 ? records.Max(record => record.CreatedTimestamp) : null,
                    Values = records
                });
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), LogType.Error, nameof(GetRecords), e);

                return InternalServerError(e);
            }
        }

        [Description("Gets records based on the creation date condition provided.")]
        [WebApiRequest(WebApiRequestMethod.Get, "{topicId}/{startTimestamp}/{endTimestamp}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(RecordResponseDTO))]
        // ReSharper disable once TooManyDeclarations
        public WebApiResponse GetRecords(
            [Description("The identification string of the DataTopic.")]
            string topicId,
            [Description("Creation date start point.")]
            long startTimestamp,
            [Description("Creation date end point.")]
            long endTimestamp)
        {
            try
            {
                var topic = _dataStore.GetTopic(topicId);

                if (topic == null)
                {
                    return NotFound();
                }

                var records = GetCreatedRecords(_dataStore, topic, new DateTime(startTimestamp),
                        new DateTime(endTimestamp), null)
                    .Take(MaximumRecords).ToArray();

                return Ok(new RecordResponseDTO
                {
                    Count = records.Length,
                    Start = records.Length > 0 ? records.Min(record => record.CreatedTimestamp) : null,
                    End = records.Length > 0 ? records.Max(record => record.CreatedTimestamp) : null,
                    Values = records
                });
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataRecordsController), LogType.Error, nameof(GetRecords), e);

                return InternalServerError(e);
            }
        }
    }
}