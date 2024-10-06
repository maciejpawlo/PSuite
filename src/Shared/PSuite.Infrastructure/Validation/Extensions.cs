using System.Reflection;
using System.Runtime.CompilerServices;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure.Validation;
internal static class Extensions
{
    internal static IServiceCollection AddValidation(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);
        return services;
    }
}
