using Microsoft.Extensions.DependencyInjection;

namespace PSuite.Shared.Infrastructure.Cache;

internal static class Extensions
{
    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        var options = services.GetOptions<RedisOptions>(RedisOptions.SectionName);
        services
            .AddDistributedMemoryCache()
            .AddStackExchangeRedisCache(setup => 
            {
                setup.Configuration = options.ConnectionString;
            });
        services.AddMemoryCache();
        return services;
    }
}
