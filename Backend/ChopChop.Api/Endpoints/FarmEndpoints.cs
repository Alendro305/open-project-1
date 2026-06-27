using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Services;

namespace ChopChop.Api.Endpoints;

public static class FarmEndpoints
{
    public static IEndpointRouteBuilder MapFarmEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/farm").WithTags("Farming").RequireAuthorization();

        // Seed catalog is public-ish (still requires auth) so the client can show grow times.
        group.MapGet("/seeds", (ISeedCatalog seeds) => Results.Ok(seeds.All));

        group.MapGet("/rooms/{roomId}/plots", async (string roomId, IFarmService farm) =>
            Results.Ok(await farm.ListAsync(roomId)));

        group.MapPost("/rooms/{roomId}/plots", async (string roomId, PlacePlotRequest req, ClaimsPrincipal user, IFarmService farm) =>
            ToResult(await farm.PlaceAsync(user.GetUserIdOrThrow(), roomId, req.x, req.y, req.z)));

        group.MapPost("/plots/{plotId}/plant", async (string plotId, PlantRequest req, ClaimsPrincipal user, IFarmService farm) =>
            ToResult(await farm.PlantAsync(user.GetUserIdOrThrow(), plotId, req.SeedItemId)));

        group.MapPost("/plots/{plotId}/water", async (string plotId, ClaimsPrincipal user, IFarmService farm) =>
            ToResult(await farm.WaterAsync(user.GetUserIdOrThrow(), plotId)));

        group.MapPost("/plots/{plotId}/harvest", async (string plotId, ClaimsPrincipal user, IFarmService farm) =>
            ToResult(await farm.HarvestAsync(user.GetUserIdOrThrow(), plotId)));

        group.MapDelete("/plots/{plotId}", async (string plotId, ClaimsPrincipal user, IFarmService farm) =>
        {
            var r = await farm.RemoveAsync(user.GetUserIdOrThrow(), plotId);
            return r.Ok ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
        });

        return app;
    }

    private static IResult ToResult(Result<FarmPlotDto> r)
        => r.Ok ? Results.Ok(r.Value) : Results.BadRequest(new { error = r.Error });
}
