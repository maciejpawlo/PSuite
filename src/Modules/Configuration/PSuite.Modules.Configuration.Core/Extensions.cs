using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Keycloak;
using PSuite.Modules.Configuration.Core.Services;
using PSuite.Shared.Infrastructure.Database;
using Microsoft.Extensions.Options;
using PSuite.Modules.Configuration.Core.Configuration;

[assembly: InternalsVisibleTo("PSuite.Modules.Configuration.Api")]
namespace PSuite.Modules.Configuration.Core;
internal static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        services.Configure<KeycloakOptions>(options =>
        {
            var section = configuration.GetSection(KeycloakOptions.SectionName);
            options.Url = section.GetValue<string>("auth-server-url")!;
            options.ClientSecret = section.GetValue<string>("credentials:secret")!;
            options.Realm = section.GetValue<string>(nameof(options.Realm))!;
            options.Resource = section.GetValue<string>(nameof(options.Resource))!;
        });
        
        services.AddTransient<KeycloakAuthMessageHandler>();
        services.AddHttpClient<IKeycloakService, KeycloakService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        })
        .AddHttpMessageHandler<KeycloakAuthMessageHandler>();

        services.AddPostgres<ConfigurationDbContext>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }
}
