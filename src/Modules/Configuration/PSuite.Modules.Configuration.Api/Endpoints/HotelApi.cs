using Keycloak.AuthServices.Authorization;
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
            .WithMetadata();

        hotelEndpoints.MapPost("", (IHotelService hotelService, HotelDto request) => hotelService.CreateAsync(request))
            .WithName("Create hotel")
            .RequireProtectedResource("configuration", "configuration:write");
        
        hotelEndpoints.MapPut("/{id:guid}", (IHotelService hotelService, HotelDto request, Guid id) => hotelService.UpdateAsync(request))
            .WithName("Update hotel")
            .RequireProtectedResource("configuration", "configuration:write");

        hotelEndpoints.MapDelete("/{id:guid}", (IHotelService hotelService, Guid id) => hotelService.DeleteAsync(id))
            .WithName("Delete hotel")
            .RequireProtectedResource("configuration", "configuration:write");
            
        hotelEndpoints.MapGet("", (IHotelService hotelService) => hotelService.GetAllAsync())
            .WithName("Get all hotels")
            .RequireProtectedResource("configuration", "configuration:read");

        hotelEndpoints.MapGet("/{id:guid}", (IHotelService hotelService, Guid id) => hotelService.GetByIdAsync(id))
            .WithName("Get hotel by id")
            .RequireProtectedResource("configuration", "configuration:read");
    }
}
