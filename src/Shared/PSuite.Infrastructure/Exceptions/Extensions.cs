using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure.Exceptions;
internal static class Extensions
{
    internal static IServiceCollection AddExceptions(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    internal static void UseExceptions(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
    }
}
