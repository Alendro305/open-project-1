using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Services;

namespace ChopChop.Api.Endpoints;

public static class TradeEndpoints
{
    public static IEndpointRouteBuilder MapTradeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trades").WithTags("Trading").RequireAuthorization();

        group.MapPost("/", async (CreateTradeRequest req, ClaimsPrincipal user, ITradeService trades) =>
            ToResult(await trades.CreateAsync(user.GetUserIdOrThrow(), req.RoomId, req.ResponderUserId)));

        group.MapGet("/{tradeId}", async (string tradeId, ITradeService trades) =>
        {
            var state = await trades.GetStateAsync(tradeId);
            return state is null ? Results.NotFound() : Results.Ok(state);
        });

        group.MapPut("/{tradeId}/offer", async (string tradeId, SetOfferRequest req, ClaimsPrincipal user, ITradeService trades) =>
            ToResult(await trades.SetOfferAsync(user.GetUserIdOrThrow(), tradeId, req.Coins, req.Items)));

        group.MapPost("/{tradeId}/confirm", async (string tradeId, ClaimsPrincipal user, ITradeService trades) =>
            ToResult(await trades.ConfirmAsync(user.GetUserIdOrThrow(), tradeId)));

        group.MapPost("/{tradeId}/cancel", async (string tradeId, ClaimsPrincipal user, ITradeService trades) =>
            ToResult(await trades.CancelAsync(user.GetUserIdOrThrow(), tradeId)));

        return app;
    }

    private static IResult ToResult(Result<TradeStateDto> r)
        => r.Ok ? Results.Ok(r.Value) : Results.BadRequest(new { error = r.Error });
}
