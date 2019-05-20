using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using Newtonsoft.Json;
using NJsonSchema;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration.Processors.Security;
using Owin;
using SharpWorker.Log;
using SharpWorker.WebApi;

namespace SharpWorker.NetCoordinator.WebApi
{
    internal class WebApiStartup
    {
        private readonly bool _isSwaggerEnable;
        private readonly bool _isSwaggerUIEnable;
        private readonly Logger _logger;
        private readonly WebApiProxyControllerService _webApiService;

        // ReSharper disable once TooManyDependencies
        public WebApiStartup(
            WebApiProxyControllerService webApiService,
            Logger logger,
            bool isSwaggerEnable,
            bool isSwaggerUIEnable)
        {
            _webApiService = webApiService;
            _logger = logger;
            _isSwaggerEnable = isSwaggerEnable;
            _isSwaggerUIEnable = isSwaggerUIEnable;
        }

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            config.Services.Add(typeof(IExceptionLogger), new WebApiTraceExceptionLogger(_logger));
            config.Services.Replace(typeof(IHttpControllerActivator), _webApiService);
            config.Services.Replace(typeof(IHttpControllerTypeResolver), _webApiService);
            config.Services.Replace(typeof(IAssembliesResolver), _webApiService);

            // ReSharper disable once EventExceptionNotDocumented
            config.Formatters.JsonFormatter.SerializerSettings = JsonConvert.DefaultSettings.Invoke();

            config.MapHttpAttributeRoutes();

            if (_isSwaggerEnable)
            {
                // Swagger
                app.UseSwagger(_webApiService.GetAllControllerTypes(), settings =>
                {
                    var apiDescription = (_webApiService as IWebApiWorkerService)?.GetApiDescription();

                    settings.GeneratorSettings.Title = Assembly.GetEntryAssembly().GetName(false).Name;
                    settings.GeneratorSettings.Version = Assembly.GetEntryAssembly().GetName(false).Version.ToString(3);
                    settings.GeneratorSettings.Description =
                        Assembly
                            .GetExecutingAssembly()
                            .GetCustomAttributes(
                                typeof(AssemblyDescriptionAttribute), false)
                            .OfType<AssemblyDescriptionAttribute>()
                            .FirstOrDefault()?
                            .Description ??
                        "";

                    settings.GeneratorSettings.DefaultUrlTemplate = _webApiService.BasePath + "/{controller}/{id}";
                    settings.GeneratorSettings.SchemaType = SchemaType.OpenApi3;
                    settings.GeneratorSettings.OperationProcessors.Add(new SwaggerFileResponseOperationProcessor());
                    settings.GeneratorSettings.OperationProcessors.Add(
                        new SwaggerOperationProcessor(
                            apiDescription
                        )
                    );
                    settings.GeneratorSettings.DocumentProcessors.Add(
                        new SecurityDefinitionAppender(
                            "BearerAuth",
                            new SwaggerSecurityScheme
                            {
                                Type = SwaggerSecuritySchemeType.Http,
                                Scheme = "bearer",
                                Name = "Authorization",
                                Description = "A valid JWT token with necessary scopes to perform an operation.",
                                In = SwaggerSecurityApiKeyLocation.Header,
                                BearerFormat = "JWT",
                                Scopes = apiDescription
                                    .SelectMany(controller => controller.Actions.Select(action => action.AccessScope))
                                    .Distinct().ToDictionary(s => s, s => "")
                            }
                        )
                    );
                    settings.GeneratorSettings.SchemaProcessors.Add(new SwaggerSchemaProcessor(_logger));
                });

                if (_isSwaggerUIEnable)
                {
                    app.UseSwaggerUi3();
                }
            }

            app.UseWebApi(config);
        }
    }
}