using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Services;
using PSuite.Shared.Infrastructure.Validation;

namespace PSuite.Modules.Configuration.Api.Endpoints;

internal static class RoomApi
{
    internal static void RegisterRoomApi(this IEndpointRouteBuilder app, string basePath)
    {
        var roomEndpoints = app.MapGroup($"{basePath}/rooms")
            .WithTags("Room")
            .WithOpenApi()
            .WithMetadata();
        
        roomEndpoints.MapPost("", (IRoomService roomService, RoomDto request) => roomService.CreateAsync(request))
            .WithName("Create room")
            .WithFluentValidation<RoomDto>()
            .RequireProtectedResource("configuration", "configuration:write");;
        
        roomEndpoints.MapPut("/{id:guid}", (IRoomService roomService, RoomDto request, Guid id) => roomService.UpdateAsync(request))
            .WithName("Update room")
            .WithFluentValidation<RoomDto>()
            .RequireProtectedResource("configuration", "configuration:write");

        roomEndpoints.MapDelete("/{id:guid}", (IRoomService roomService, Guid id) => roomService.DeleteAsync(id))
            .WithName("Delete room")
            .RequireProtectedResource("configuration", "configuration:write");

        roomEndpoints.MapGet("", (IRoomService roomService) => roomService.GetAllAsync())
            .WithName("Get all rooms")
            .RequireProtectedResource("configuration", "configuration:read");

        roomEndpoints.MapGet("/{id:guid}", (IRoomService roomService, Guid id) => roomService.GetByIdAsync(id))
            .WithName("Get room by id")
            .RequireProtectedResource("configuration", "configuration:read");
    }
}
