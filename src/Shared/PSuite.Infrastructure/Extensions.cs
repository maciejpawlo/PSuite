using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Authentication;
using PSuite.Shared.Infrastructure.Exceptions;
using PSuite.Shared.Infrastructure.Modules;
using PSuite.Shared.Infrastructure.Cache;
using PSuite.Shared.Infrastructure.Validation;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure;
internal static class Extensions
{
    internal static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IEnumerable<IModule> modules, IEnumerable<Assembly> assemblies)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "PSuite API",
                    Version = "v1",
                    Description = "PSuite Modular Monolith API"
                };
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
        services.AddModuleInfo(modules);
        var authOptions = services.GetOptions<AuthOptions>(AuthOptions.SectionName);
        services.AddAuth(authOptions);
        services.AddCache();
        services.AddExceptions();
        services.AddValidation(assemblies);
        return services;
    }

    internal static void UseInfrastructure(this IApplicationBuilder app)
    {
        app.UseExceptions();
    }

    public static T GetOptions<T>(this IServiceCollection services, string sectionName) where T : new()
    {
        using var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return configuration.GetSection(sectionName).Get<T>() ?? new();
    }
}
