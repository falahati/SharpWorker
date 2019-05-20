using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NJsonSchema;
using NSwag;
using NSwag.Annotations;
using NSwag.SwaggerGeneration.Processors;
using NSwag.SwaggerGeneration.Processors.Contexts;

namespace SharpWorker.NetCoordinator.WebApi
{
    internal class SwaggerFileResponseOperationProcessor : IOperationProcessor
    {
        /// <inheritdoc />
        public Task<bool> ProcessAsync(OperationProcessorContext context)
        {
            var responseAttributes = context?.MethodInfo
                ?.GetCustomAttributes(typeof(SwaggerResponseAttribute), false)
                .Cast<SwaggerResponseAttribute>()
                .Where(attribute => attribute.Type == typeof(byte[]))
                .OrderBy(attr => attr.StatusCode).ToArray();

            if (responseAttributes?.Any() == true)
            {
                context.OperationDescription.Operation.Responses.Clear();
                context.OperationDescription.Operation.Produces = new List<string> {"application/octet-stream"};

                foreach (var attr in responseAttributes)
                {
                    var responseSchema = new JsonSchema4
                    {
                        Type = JsonObjectType.File,
                        Format = "byte"
                    };
                    context.OperationDescription.Operation.Responses.Add(new KeyValuePair<string, SwaggerResponse>(
                        attr.StatusCode, new SwaggerResponse
                        {
                            Description = attr.Description ?? InferDescriptionFrom(attr.StatusCode),
                            Schema = responseSchema
                        }));
                }
            }

            return Task.FromResult(true);
        }

        private string InferDescriptionFrom(string statusCode)
        {
            if (Enum.TryParse(statusCode, true, out HttpStatusCode enumValue))
            {
                return enumValue.ToString();
            }

            return null;
        }
    }

}