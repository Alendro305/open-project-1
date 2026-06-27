using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Services;

namespace ChopChop.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory").WithTags("Inventory").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, IInventoryService inv) =>
        {
            var items = await inv.GetAsync(user.GetUserIdOrThrow());
            return Results.Ok(items.Select(i => new InventoryItemDto(i.ItemId, i.Quantity)));
        });

        // Dev/test grant so trading & planting can be exercised without other content systems.
        group.MapPost("/grant", async (GrantItemRequest req, ClaimsPrincipal user, IInventoryService inv) =>
        {
            await inv.AddAsync(user.GetUserIdOrThrow(), req.ItemId, req.Quantity);
            return Results.Ok(new InventoryItemDto(req.ItemId, req.Quantity));
        });

        return app;
    }

    public record GrantItemRequest(string ItemId, int Quantity);
}
