using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Services;

namespace ChopChop.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms").RequireAuthorization();

        group.MapGet("/", async (IRoomService rooms) => Results.Ok(await rooms.ListAsync()));

        group.MapPost("/", async (CreateRoomRequest req, ClaimsPrincipal user, IRoomService rooms) =>
        {
            var state = await rooms.CreateAsync(user.GetUserIdOrThrow(), user.GetDisplayName(), req.Name, req.Capacity);
            return Results.Ok(state);
        });

        group.MapGet("/{roomId}", async (string roomId, IRoomService rooms) =>
        {
            var state = await rooms.GetStateAsync(roomId);
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPost("/{roomId}/join", async (string roomId, ClaimsPrincipal user, IRoomService rooms) =>
        {
            var (ok, error, state) = await rooms.JoinAsync(user.GetUserIdOrThrow(), user.GetDisplayName(), roomId);
            return ok ? Results.Ok(state) : Results.BadRequest(new { error });
        });

        group.MapPost("/{roomId}/leave", async (string roomId, ClaimsPrincipal user, IRoomService rooms) =>
        {
            await rooms.LeaveAsync(user.GetUserIdOrThrow(), roomId);
            return Results.NoContent();
        });

        return app;
    }
}
