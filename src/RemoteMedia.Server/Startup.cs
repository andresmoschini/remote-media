using MakingSense.AspNet.Documentation;
using MakingSense.AspNet.HypermediaApi.Formatters;
using MakingSense.AspNet.HypermediaApi.Metadata;
using MakingSense.AspNet.HypermediaApi.ValidationFilters;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RemoteMedia.Server
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            builder.AddEnvironmentVariables();
            Configuration = builder.Build().ReloadOnChanged("appsettings.json");
        }

        public IConfigurationRoot Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.OutputFormatters.Clear();
                options.OutputFormatters.Add(new HypermediaApiJsonOutputFormatter());

                options.InputFormatters.Clear();
                var inputFormatter = new HypermediaApiJsonInputFormatter();
                inputFormatter.AcceptedContentTypes.Add("text/plain");
                options.InputFormatters.Add(inputFormatter);

                // TODO: automatize filter discovering and registering using reflection (take into account standard and service filters)
                options.Filters.Add(new PayloadValidationFilter());
                options.Filters.Add(new RequiredPayloadFilter());
            });

            services.AddApiServices();
            services.AddApiMappers(typeof(Startup).GetTypeInfo().Assembly);

            // TODO: take into account these
            // services.AddScoped<CacheDirectory>();
            // services.AddLinkHelper<LinkHelper>();
            // services.AddLogging();

            services.AddCors(options =>
                options.AddPolicy("Default", builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()));

            services.AddSwaggerGen();

            services.ConfigureSwaggerDocument(options =>
            {
                options.SingleApiVersion(new Swashbuckle.SwaggerGen.Info()
                {
                    Version = "current",
                    Title = "Relay API"
                });
                // TODO: take into account these
                // options.OperationFilter<RelationMetadataOperationFilter>();
                // options.DocumentFilter<DocumentFilter>();
            });
            services.ConfigureSwaggerSchema(options =>
            {
                // TODO: move this and other code to a new independent project
                options.DescribeAllEnumsAsStrings = true;
                // TODO: take into account this
                // options.ModelFilter<LinkModelFilter>();
                options.CustomSchemaIds(T =>
                    T.GetTypeInfo()
                    .GetCustomAttributes(false)
                    .OfType<SchemaAttribute>()
                    .FirstOrDefault()?.SchemaName
                    ?? T.Name);
            });
        }

        public void Configure(IApplicationBuilder app, IApplicationEnvironment appEnv, IHostingEnvironment hostEnv, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseIISPlatformHandler();

            app.UseApiErrorHandler();

            app.UseMvc();

            app.UseSwaggerGen("swagger/{apiVersion}/swagger.json");

            app.UseSwaggerUi("swagger/ui");

            var documentationFilesProvider = new PhysicalFileProvider(appEnv.ApplicationBasePath);
            app.UseDocumentation(new DocumentationOptions()
            {
                DefaultFileName = "index",
                RequestPath = "/docs",
                NotFoundHtmlFile = documentationFilesProvider.GetFileInfo("DocumentationTemplates\\NotFound.html"),
                LayoutFile = documentationFilesProvider.GetFileInfo("DocumentationTemplates\\Layout.html")
            });

            app.UseNotFoundHandler();
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
