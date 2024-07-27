using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Services;

namespace PSuite.Modules.Configuration.Api.Endpoints;
internal static class HotelApi
{
    internal static void RegisterHotelApi(this IEndpointRouteBuilder app, string basePath)
    {
        var hotelEndpoints = app.MapGroup($"{basePath}/hotel")
            .WithTags("Hotel")
            .WithOpenApi()
            .WithMetadata();

        hotelEndpoints.MapPost("", (IHotelService hotelService, HotelDto request) => hotelService.CreateAsync(request))
            .WithName("Create hotel")
            .RequireAuthorization();
        
        hotelEndpoints.MapPut("/{id:guid}", (IHotelService hotelService, HotelDto request, Guid id) => hotelService.UpdateAsync(request))
            .WithName("Update hotel")
            .RequireAuthorization();

        hotelEndpoints.MapDelete("/{id:guid}", (IHotelService hotelService, Guid id) => hotelService.DeleteAsync(id))
            .WithName("Delete hotel")
            .RequireAuthorization();

        hotelEndpoints.MapGet("", (IHotelService hotelService) => hotelService.GetAllAsync())
            .WithName("Get all hotels")
            .RequireAuthorization();

        hotelEndpoints.MapGet("/{id:guid}", (IHotelService hotelService, Guid id) => hotelService.GetByIdAsync(id))
            .WithName("Get hotel by id")
            .RequireAuthorization();
    }
}
