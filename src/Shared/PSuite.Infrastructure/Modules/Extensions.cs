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
}
