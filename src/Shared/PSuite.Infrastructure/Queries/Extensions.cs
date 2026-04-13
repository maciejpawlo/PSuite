using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Shared.Infrastructure.Queries;

internal static class Extensions
{
    internal static IServiceCollection AddQueries(this IServiceCollection services, IList<Assembly> assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        return services;
    }
}