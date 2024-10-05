using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PSuite.Shared.Infrastructure.Database;
public static class Extensions
{
    public static IServiceCollection AddPostgres<T>(this IServiceCollection services) where T : DbContext
    {
        var postgresOptions = services.GetOptions<PostgresOptions>(PostgresOptions.SectionName);
        services.AddDbContext<T>(options => 
        {
            options.UseNpgsql(postgresOptions.ConnectionString);
        });
        return services;
    }
}
