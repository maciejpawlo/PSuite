using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure.Database;
internal static class Extensions
{
    public static IServiceCollection AddPostgres<T>(this IServiceCollection services, PostgresOptions postgresOptions) where T : DbContext
    {
        services.AddDbContext<T>(options => 
        {
            options.UseNpgsql(postgresOptions.ConnectionString);
        });
        return services;
    }
}
