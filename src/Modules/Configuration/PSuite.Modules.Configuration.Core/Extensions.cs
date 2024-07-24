using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Modules.Configuration.Core.Services;

[assembly: InternalsVisibleTo("PSuite.Modules.Configuration.Api")]
namespace PSuite.Modules.Configuration.Core;
internal static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }
}
