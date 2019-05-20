using System.Linq;
using System.Threading.Tasks;
using NSwag;
using NSwag.SwaggerGeneration.Processors;
using NSwag.SwaggerGeneration.Processors.Contexts;
using SharpWorker.WebApi.Description;

namespace SharpWorker.NetCoordinator.WebApi
{
    internal class SwaggerOperationProcessor : IOperationProcessor
    {
        private WebApiControllerDescription[] webApiControllerDescription;

        public SwaggerOperationProcessor(WebApiControllerDescription[] webApiControllerDescription)
        {
            this.webApiControllerDescription = webApiControllerDescription;
        }

        /// <inheritdoc />
        public Task<bool> ProcessAsync(OperationProcessorContext context)
        {

            var actionDescription = webApiControllerDescription
                ?.FirstOrDefault(controller => controller?.Name == context?.ControllerType?.Name)?.Actions
                ?.FirstOrDefault(action => action?.Name == context?.MethodInfo?.Name);

            if (actionDescription != null)
            {
                foreach (var contextParameter in context.Parameters)
                {
                    var parameterDescription =
                        actionDescription.Parameters.FirstOrDefault(parameter =>
                            parameter.Name == contextParameter.Value.Name);

                    if (parameterDescription != null)
                    {
                        contextParameter.Value.IsRequired = !parameterDescription.IsOptional;
                    }
                }

                context.OperationDescription.Operation.Security = new[]
                {
                    new SwaggerSecurityRequirement
                    {
                        {"BearerAuth", new[] {actionDescription.AccessScope}}
                    }
                };
            }

            return Task.FromResult(true);
        }
    }
}