using Microsoft.Framework.Internal;
using RemoteMedia.Server.ApiServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DefaultApiServicesServiceCollectionExtensions
    {
        /// <summary>
        /// Search and register all concrete types in the namespace of `IListsApiService` (`Doppler.HypermediaAPI.ApiServices`)
        /// If it has interfaces in the same namespace, it uses them as type and the concrete class as implementation type.
        /// </summary>
        public static IServiceCollection AddApiServices([NotNull] this IServiceCollection services)
        {
            var arbitraryServiceInterface = typeof(IDummyApiService);
            var targetAssembly = arbitraryServiceInterface.GetTypeInfo().Assembly;
            var targetNamespace = arbitraryServiceInterface.GetTypeInfo().Namespace;
            var serviceTypes = targetAssembly.GetTypes()
                .Select(x => new { Type = x, Info = x.GetTypeInfo() })
                .Where(x => !x.Info.IsAbstract
                    && !x.Info.IsInterface
                    && x.Info.IsPublic
                    && x.Info.Namespace != null
                    && x.Info.Namespace.StartsWith(targetNamespace, StringComparison.Ordinal));

            foreach (var serviceType in serviceTypes)
            {
                var serviceInterfaces = serviceType.Type.GetInterfaces()
                    .Where(x => x.Namespace.StartsWith(targetNamespace))
                    .ToArray();

                if (serviceInterfaces.Length == 0)
                {
                    services.AddScoped(serviceType.Type);
                }
                else
                {
                    foreach (var service in serviceInterfaces)
                    {
                        services.AddScoped(service, serviceType.Type);
                    }
                }
            }

            return services;
        }
    }
}
