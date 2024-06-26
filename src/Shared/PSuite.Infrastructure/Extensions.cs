using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Authentication;
using PSuite.Shared.Infrastructure.Modules;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure;
internal static class Extensions
{
    internal static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IEnumerable<IModule> modules, IEnumerable<Assembly> assemblies, IConfiguration configuration)
    {
        services.AddModuleInfo(modules);
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>();
        services.AddAuth(authOptions);
        return services;
    }

    internal static void UseInfrastructure(this IApplicationBuilder app)
    {
        
    }
}
