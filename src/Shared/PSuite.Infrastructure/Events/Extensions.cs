using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Events;

namespace PSuite.Shared.Infrastructure.Events;

internal static class Extensions
{
    internal static IServiceCollection AddEvents(this IServiceCollection services, IList<Assembly> assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

        services.AddScoped<IEventDispatcher, EventDispatcher>();

        return services;
    }
}
