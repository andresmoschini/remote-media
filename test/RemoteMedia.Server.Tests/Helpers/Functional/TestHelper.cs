using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using RemoteMedia.Server.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace RemoteMedia.Server.Tests.Helpers.Functional
{
    public class DummyLoggingConfigurationSection : IConfigurationSection
    {
        public string this[string key]
        {
            get { return null; }
            set { }
        }

        public string Key => "Logging";

        public string Path => "Logging";

        public string Value
        {
            get { return null; }
            set { }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => null;

        public IConfigurationSection GetSection(string key) => null;
    }

    public static class TestHelper
    {
        public static TestServer CreateRelayServer(Action<RemoteMediaConfiguration> overrideConfiguration = null, Action<IServiceCollection> overrideServices = null)
        {
            var configuration = new RemoteMediaConfiguration()
            {
                mediaFiles = new MediaFilesConfiguration()
                {
                    collections = new[]
                    {
                        new MediaFilesCollectionConfiguration()
                        {
                            name = "Videos",
                            path = "/media/shared/videos",
                            mediaTypes = new[] { MediaType.video }
                        },
                        new MediaFilesCollectionConfiguration()
                        {
                            name = "Music",
                            path = "/media/shared/music",
                            mediaTypes = new[] { MediaType.audio }
                        },
                        new MediaFilesCollectionConfiguration()
                        {
                            name = "Photos",
                            path = "/media/shared/photos",
                            mediaTypes = new[] { MediaType.image, MediaType.video }
                        }
                    },
                    audioFileExtensions = new[] { "aac", "aif", "aifc", "aiff", "amr", "ast", "flac", "gsm", "m4a", "m4p", "mp2", "mp3", "mp4", "ots", "ra", "rm", "swa", "wav", "wma" },
                    imageFileExtensions = new[] { "bmp", "djvu", "gif", "jp2", "jpeg", "jpg", "jps", "mng", "pcx", "png", "raw", "tif", "tiff" },
                    videoFileExtensions = new[] { "3gp", "aaf", "asf", "avi", "fla", "flv", "m1v", "m2v", "m4v", "mkv", "mov", "mp4", "mpe", "mpeg", "mpg", "mxf", "nsv", "ogg", "rm", "svi", "swf", "wmv", "wtv", "yuv" }
                }
            };

            overrideConfiguration?.Invoke(configuration);

            var startup = new Startup(null);
            startup.RemoteMediaConfiguration = configuration;
            startup.LogginConfiguration = new DummyLoggingConfigurationSection();


            // TODO: Personalize CreateServer to configure NullLoggerFactory and other personalizations for tests
            // Allow to add mocks
            var server = CreateServer(
                app => startup.Configure(
                    app,
                    app.ApplicationServices.GetService<IApplicationEnvironment>(),
                    app.ApplicationServices.GetService<ILoggerFactory>()),
                "Relay.Application.Api",
                services =>
                {
                    startup.ConfigureServices(services);
                    overrideServices?.Invoke(services);
                });

            return server;
        }

        public static TestServer CreateServer(Action<IApplicationBuilder> builder, string applicationWebSiteName)
        {
            return CreateServer(
                builder,
                applicationWebSiteName,
                applicationPath: null,
                configureServices: (Action<IServiceCollection>)null);
        }

        public static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            Action<IServiceCollection> configureServices)
        {
            return CreateServer(
                builder,
                applicationWebSiteName,
                applicationPath: null,
                configureServices: configureServices);
        }

        private static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            string applicationPath,
            Action<IServiceCollection> configureServices)
        {
            return TestServer.Create(
                builder,
                services => AddTestServices(services, applicationWebSiteName, applicationPath, configureServices));
        }

        private static void AddTestServices(
            IServiceCollection services,
            string applicationWebSiteName,
            string applicationPath,
            Action<IServiceCollection> configureServices)
        {
            applicationPath = applicationPath ?? "..";

            // Get current IApplicationEnvironment; likely added by the host.
            var provider = services.BuildServiceProvider();
            var originalEnvironment = provider.GetRequiredService<IApplicationEnvironment>();

            // When an application executes in a regular context, the application base path points to the root
            // directory where the application is located, for example MvcSample.Web. However, when executing
            // an application as part of a test, the ApplicationBasePath of the IApplicationEnvironment points
            // to the root folder of the test project.
            // To compensate for this, we need to calculate the original path and override the application
            // environment value so that components like the view engine work properly in the context of the
            // test.
            var applicationBasePath = CalculateApplicationBasePath(
                originalEnvironment,
                applicationWebSiteName,
                applicationPath);
            var environment = new TestApplicationEnvironment(
                originalEnvironment,
                applicationWebSiteName,
                applicationBasePath);
            services.AddInstance<IApplicationEnvironment>(environment);
            var hostingEnvironment = new HostingEnvironment();
            hostingEnvironment.Initialize(applicationBasePath, config: null);
            services.AddInstance<IHostingEnvironment>(hostingEnvironment);

            // Injecting a custom assembly provider. Overrides AddMvc() because that uses TryAdd().
            var assemblyProvider = CreateAssemblyProvider(applicationWebSiteName);
            services.AddInstance(assemblyProvider);

            // Avoid using pooled memory, we don't have a guarantee that our services will get disposed.
            services.AddInstance<IHttpResponseStreamWriterFactory>(new TestHttpResponseStreamWriterFactory());

            if (configureServices != null)
            {
                configureServices(services);
            }
        }

        // Calculate the path relative to the application base path.
        private static string CalculateApplicationBasePath(
            IApplicationEnvironment appEnvironment,
            string applicationWebSiteName,
            string websitePath)
        {
            // Mvc/test/WebSites/applicationWebSiteName
            return Path.GetFullPath(
                Path.Combine(appEnvironment.ApplicationBasePath, websitePath, applicationWebSiteName));
        }

        private static IAssemblyProvider CreateAssemblyProvider(string siteName)
        {
            // Creates a service type that will limit MVC to only the controllers in the test site.
            // We only want this to happen when running in-process.
            var assembly = Assembly.Load(new AssemblyName(siteName));
            var provider = new StaticAssemblyProvider
            {
                CandidateAssemblies =
                {
                    assembly,
                },
            };

            return provider;
        }
    }
}
