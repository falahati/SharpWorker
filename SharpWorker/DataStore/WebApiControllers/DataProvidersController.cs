using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using SharpWorker.Log;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.DataStore.WebApiControllers
{
    [Description("Provides access to data providers and data topics.")]
    [WebApiController("data")]
    // ReSharper disable once HollowTypeName
    public class DataProvidersController : WebApiController
    {
        private readonly DataStoreBase _dataStore;
        private readonly Logger _logger;

        public DataProvidersController(DataStoreBase dataStore, Logger logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        [Description("Truncates all records of a data topic.")]
        [WebApiRequest(WebApiRequestMethod.Delete, "{provider}/{subject}/{parameters}", "write")]
        [WebApiResponse(HttpStatusCode.OK, typeof(int))]
        public WebApiResponse ClearTopic(
            [Description("The provider name.")] string provider,
            [Description("The provider's subject.")]
            string subject,
            [Description("The subject's parameters.")]
            string[] parameters)
        {
            try
            {
                return Ok(_dataStore.ClearTopic(provider, subject, parameters));
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataProvidersController), LogType.Error, nameof(ClearTopic), e);

                return InternalServerError(e);
            }
        }


        [Description("Gets the list of all subject's parameters.")]
        [WebApiRequest(WebApiRequestMethod.Get, "{provider}/{subject}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(string[][]))]
        public WebApiResponse GetParameters(
            [Description("The provider name.")] string provider,
            [Description("The provider's subject.")]
            string subject)
        {
            try
            {
                return Ok(_dataStore.GetTopics(provider, subject).Select(topic => topic.Parameters).Distinct());
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataProvidersController), LogType.Error, nameof(GetParameters), e);

                return InternalServerError(e);
            }
        }


        [Description("Gets the list of all providers.")]
        [WebApiRequest(WebApiRequestMethod.Get, "", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(string[]))]
        public WebApiResponse GetProviders()
        {
            try
            {
                return Ok(_dataStore.GetTopics().Select(topic => topic.Provider.ToString()).Distinct());
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataProvidersController), LogType.Error, nameof(GetProviders), e);

                return InternalServerError(e);
            }
        }

        [Description("Gets the list of all provider's subjects.")]
        [WebApiRequest(WebApiRequestMethod.Get, "{provider}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(string[]))]
        public WebApiResponse GetSubjects([Description("The provider name.")] string provider)
        {
            try
            {
                return Ok(_dataStore.GetTopics(provider).Select(topic => topic.Subject).Distinct());
            }

            catch (Exception e)
            {
                _logger.Log(nameof(DataProvidersController), LogType.Error, nameof(GetSubjects), e);

                return InternalServerError(e);
            }
        }

        [Description("Gets the data topic for the passed parameters.")]
        [WebApiRequest(WebApiRequestMethod.Get, "{provider}/{subject}/{parameters}", "read")]
        [WebApiResponse(HttpStatusCode.OK, typeof(DataTopic))]
        public WebApiResponse GetTopic(
            [Description("The provider name.")] string provider,
            [Description("The provider's subject.")]
            string subject,
            [Description("The subject's parameters.")]
            string[] parameters)
        {
            try
            {
                return Ok(_dataStore.GetTopic(provider, subject, parameters));
            }
            catch (Exception e)
            {
                _logger.Log(nameof(DataProvidersController), LogType.Error, nameof(GetTopic), e);

                return InternalServerError(e);
            }
        }
    }
}