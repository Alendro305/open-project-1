using ChopChop.Api.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace ChopChop.Api.Realtime;

/// <summary>Pushes server-side state changes to connected clients over <see cref="GameHub"/>.</summary>
public interface IRealtimeNotifier
{
    Task RoomUpdated(RoomStateDto room);
    Task PlotsUpdated(string roomId, IReadOnlyList<FarmPlotDto> plots);
    Task TradeUpdated(TradeStateDto trade);
    Task TradeInvited(string responderUserId, TradeStateDto trade);
}

public sealed class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<GameHub> _hub;

    public RealtimeNotifier(IHubContext<GameHub> hub) => _hub = hub;

    public Task RoomUpdated(RoomStateDto room)
        => _hub.Clients.Group(GameHub.RoomGroup + room.Id).SendAsync("RoomUpdated", room);

    public Task PlotsUpdated(string roomId, IReadOnlyList<FarmPlotDto> plots)
        => _hub.Clients.Group(GameHub.RoomGroup + roomId).SendAsync("PlotsUpdated", roomId, plots);

    public Task TradeUpdated(TradeStateDto trade)
        => _hub.Clients.Users(trade.Initiator.UserId, trade.Responder.UserId).SendAsync("TradeUpdated", trade);

    public Task TradeInvited(string responderUserId, TradeStateDto trade)
        => _hub.Clients.User(responderUserId).SendAsync("TradeInvited", trade);
}
