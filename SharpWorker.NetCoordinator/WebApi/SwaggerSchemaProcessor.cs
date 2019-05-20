using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Generation;
using SharpWorker.Log;
using SharpWorker.WebApi.Attributes;

namespace SharpWorker.NetCoordinator.WebApi
{
    // ReSharper disable once HollowTypeName
    internal class SwaggerSchemaProcessor : ISchemaProcessor
    {
        private readonly Logger _logger;

        public SwaggerSchemaProcessor(Logger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        // ReSharper disable once ExcessiveIndentation
        public async Task ProcessAsync(SchemaProcessorContext context)
        {
            try
            {
                var rootType = context.Type;
                var rootSchema = context.Schema;

                var subtypeAttributes = WebApiTypeDiscriminatorAttribute.GetSubtypes(rootType)
                    .Select(t => new KnownTypeAttribute(t))
                    .ToArray();

                if (subtypeAttributes.Any())
                {
                    foreach (var subtypeAttribute in subtypeAttributes)
                    {
                        var subType = subtypeAttribute.Type;

                        try
                        {
                            var subTypeSchema = await context.Generator.GenerateWithReferenceAsync<JsonSchema4>(
                                subType,
                                subtypeAttributes.Cast<Attribute>().ToArray(),
                                context.Resolver, (p, s) => Task.CompletedTask
                            ).ConfigureAwait(false);

                            if (rootSchema.DiscriminatorObject == null)
                            {
                                rootSchema.DiscriminatorObject = new OpenApiDiscriminator
                                {
                                    JsonInheritanceConverter = new JsonInheritanceConverter(),
                                    PropertyName = JsonInheritanceConverter.DiscriminatorName
                                };

                                rootSchema.Properties[rootSchema.DiscriminatorObject.PropertyName] = new JsonProperty
                                {
                                    Type = JsonObjectType.String,
                                    IsRequired = true
                                };
                            }

                            // Indicate that this type is a subtype of the root type
                            rootSchema.DiscriminatorObject?.AddMapping(subType, subTypeSchema);

                            // Get the real base type of this sub-type
                            var baseType = WebApiTypeDiscriminatorAttribute.GetBaseType(subType);

                            if (baseType == null)
                            {
                                continue;
                            }

                            if (baseType != rootType)
                            {
                                if (subTypeSchema.ActualSchema.AllOf.Count == 0)
                                {
                                    // If base type is different than the root type, generate the base type and
                                    // add to AllOf of sub-type's schema
                                    try
                                    {
                                        var baseTypeSchema = await context.Generator
                                            .GenerateWithReferenceAsync<JsonSchema4>(
                                                baseType,
                                                new Attribute[0],
                                                context.Resolver,
                                                (p, s) => Task.CompletedTask
                                            ).ConfigureAwait(false);

                                        if (subTypeSchema.ActualSchema.AllOf.Count == 0)
                                        {
                                            subTypeSchema.ActualSchema.AllOf.Add(baseTypeSchema);
                                        }

                                        // Indicate that this type is a subtype of the root type
                                        baseTypeSchema.DiscriminatorObject?.AddMapping(subType, subTypeSchema);
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.Log(nameof(SwaggerSchemaProcessor), LogType.Warning, e);
                                    }
                                }
                            }
                            else if (baseType == rootType)
                            {
                                if (baseType.IsInterface && subTypeSchema.ActualSchema.AllOf.Count == 0)
                                {
                                    // If base type is same as the root type, base type will be automatically
                                    // to the AllOf of sub-type's schema; unless if the base type is an interface
                                    // in that case we add it manually
                                    subTypeSchema.ActualSchema.AllOf.Add(
                                        new JsonSchema4
                                        {
                                            Reference = rootSchema
                                        }
                                    );
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Log(nameof(SwaggerSchemaProcessor), LogType.Warning, e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Log(nameof(SwaggerSchemaProcessor), LogType.Error, e);

                throw;
            }
        }
    }
}