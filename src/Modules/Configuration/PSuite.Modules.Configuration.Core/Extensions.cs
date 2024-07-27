using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DAL.Repositories;
using PSuite.Modules.Configuration.Core.Repositories;
using PSuite.Modules.Configuration.Core.Services;
using PSuite.Shared.Infrastructure.Database;

[assembly: InternalsVisibleTo("PSuite.Modules.Configuration.Api")]
namespace PSuite.Modules.Configuration.Core;
internal static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddPostgres<ConfigurationDbContext>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }
}
