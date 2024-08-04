using Microsoft.Extensions.DependencyInjection;

namespace PSuite.Shared.Infrastructure.Cache;

internal static class Extensions
{
    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }
}
