using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Commands;
using PSuite.Shared.Abstractions.Events;
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
        //services.AddSingleton<IModuleClient, ModuleClient>();
        //services.AddSingleton<IModuleSerializer, JsonModuleSerializer>();
        //services.AddSingleton<IModuleSubscriber, ModuleSubscriber>();

        return services;
    }

    private static void AddModuleRegistry(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        ModuleRegistry registry = new();
        var types = assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

        var commandTypes = types
            .Where(t => typeof(ICommand).IsAssignableFrom(t))
            .ToArray();

        var eventTypes = types
            .Where(t => typeof(IEvent).IsAssignableFrom(t))
            .ToArray();
        
        services.AddSingleton<IModuleRegistry>(sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();
            var commandDispatcherType = commandDispatcher.GetType();

            foreach (var commandType in commandTypes)
            {
                registry.AddBroadcastAction(commandType, (command, token)
                    => (Task)commandDispatcherType.GetMethod(nameof(commandDispatcher.SendAsync))!
                        .MakeGenericMethod(commandType)
                        .Invoke(commandDispatcher, [command, token])!);
            }

            var eventDispatcher = sp.GetRequiredService<IEventDispatcher>();
            var eventDispatcherType = eventDispatcher.GetType();

            foreach (var eventType in eventTypes)
            {
                registry.AddBroadcastAction(eventType, (@event, token)
                    => (Task)eventDispatcherType.GetMethod(nameof(eventDispatcher.PublishAsync))!
                        .MakeGenericMethod(eventType)
                        .Invoke(eventDispatcher, [@event, token])!);
            }
            
            return registry;
        });
    }
}
