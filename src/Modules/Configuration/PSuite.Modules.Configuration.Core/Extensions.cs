using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DAL.Repositories;
using PSuite.Modules.Configuration.Core.Keycloak;
using PSuite.Modules.Configuration.Core.Repositories;
using PSuite.Modules.Configuration.Core.Services;
using PSuite.Shared.Infrastructure.Database;

[assembly: InternalsVisibleTo("PSuite.Modules.Configuration.Api")]
namespace PSuite.Modules.Configuration.Core;
internal static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.AddTransient<KeycloakAuthMessageHandler>();
        services.AddHttpClient<KeycloakService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        })
        .AddHttpMessageHandler<KeycloakAuthMessageHandler>();

        services.AddPostgres<ConfigurationDbContext>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }
}
