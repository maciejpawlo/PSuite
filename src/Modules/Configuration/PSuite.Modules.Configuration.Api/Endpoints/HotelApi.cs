using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Commands;
using PSuite.Modules.Configuration.Core.Queries;
using PSuite.Shared.Abstractions.Commands;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Modules.Configuration.Api.Endpoints;
internal static class HotelApi
{
    internal static void RegisterHotelApi(this IEndpointRouteBuilder app, string basePath)
    {
        var hotelEndpoints = app.MapGroup($"{basePath}/hotels")
            .WithTags("Hotel")
            .WithMetadata();

        hotelEndpoints.MapPost("", (ICommandDispatcher commands, HotelDto request, CancellationToken ct) => commands.SendAsync(new CreateHotel(request.Name), ct))
            .WithName("Create hotel")
            .RequireProtectedResource("configuration", "configuration:write");
        
        hotelEndpoints.MapPut("/{id:guid}", (ICommandDispatcher commands, HotelDto request, Guid id, CancellationToken ct) => commands.SendAsync(new UpdateHotel(request.Id, request.Name), ct))
            .WithName("Update hotel")
            .RequireProtectedResource("configuration", "configuration:write");

        hotelEndpoints.MapDelete("/{id:guid}", (ICommandDispatcher commands, Guid id, CancellationToken ct) => commands.SendAsync(new DeleteHotel(id), ct))
            .WithName("Delete hotel")
            .RequireProtectedResource("configuration", "configuration:write");
            
        hotelEndpoints.MapGet("", (IQueryDispatcher queries, CancellationToken ct) => queries.DispatchAsync(new GetHotels(), ct))
            .WithName("Get all hotels")
            .RequireProtectedResource("configuration", "configuration:read");

        hotelEndpoints.MapGet("/{id:guid}", (IQueryDispatcher queries, Guid id, CancellationToken ct) => queries.DispatchAsync(new GetHotel(id), ct))
            .WithName("Get hotel by id")
            .RequireProtectedResource("configuration", "configuration:read");
    }
}
