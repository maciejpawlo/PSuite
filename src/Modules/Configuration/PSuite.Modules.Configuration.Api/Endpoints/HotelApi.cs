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
        var hotelEndpoints = app.MapGroup($"{basePath}/hotels")
            .WithTags("Hotel")
            .WithOpenApi()
            .WithMetadata()
            .RequireAuthorization();

        hotelEndpoints.MapPost("", (IHotelService hotelService, HotelDto request) => hotelService.CreateAsync(request))
            .WithName("Create hotel");
        
        hotelEndpoints.MapPut("/{id:guid}", (IHotelService hotelService, HotelDto request, Guid id) => hotelService.UpdateAsync(request))
            .WithName("Update hotel");

        hotelEndpoints.MapDelete("/{id:guid}", (IHotelService hotelService, Guid id) => hotelService.DeleteAsync(id))
            .WithName("Delete hotel");
            
        hotelEndpoints.MapGet("", (IHotelService hotelService) => hotelService.GetAllAsync())
            .WithName("Get all hotels");

        hotelEndpoints.MapGet("/{id:guid}", (IHotelService hotelService, Guid id) => hotelService.GetByIdAsync(id))
            .WithName("Get hotel by id");
    }
}
