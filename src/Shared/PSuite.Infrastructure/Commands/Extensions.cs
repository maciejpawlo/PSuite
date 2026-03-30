using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Commands;

namespace PSuite.Shared.Infrastructure.Commands;

internal static class Extensions
{
    internal static IServiceCollection AddCommands(this IServiceCollection services, IList<Assembly> assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());
        
        return services;
    }
}