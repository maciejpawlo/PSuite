using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Modules;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure.Modules;
internal static class Extensions
{
    internal static IServiceCollection AddModuleInfo(this IServiceCollection services, IEnumerable<IModule> modules)
    {
        services.AddSingleton(new ModuleInfoProvider(modules));
        return services;
    }

    internal static void MapModuleInfo(this IEndpointRouteBuilder app)
    {
        app.MapGet("modules", (ModuleInfoProvider moduleInfoProvider) => 
        {
            return moduleInfoProvider.Modules;
        });
    }
    
    internal static IServiceCollection AddModuleRequests(this IServiceCollection services, IList<Assembly> assemblies)
    {
        services.AddModuleRegistry(assemblies);
        // services.AddSingleton<IModuleClient, ModuleClient>();
        //services.AddSingleton<IModuleSerializer, JsonModuleSerializer>();
        //services.AddSingleton<IModuleSubscriber, ModuleSubscriber>();

        return services;
    }

    private static void AddModuleRegistry(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        ModuleRegistry registry = new();
        var types = assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

        /*var eventTypes = types
            .Where(x => x.IsClass && typeof(IEvent).IsAssignableFrom(x));

        foreach (var eventType in eventTypes)
        {
            // registry.AddBroadcastAction(eventType, eventTy);
        }*/
    }
}
