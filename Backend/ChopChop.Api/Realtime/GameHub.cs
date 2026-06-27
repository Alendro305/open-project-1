using System.Collections.Concurrent;
using ChopChop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChopChop.Api.Realtime;

/// <summary>
/// Single real-time hub for all online mechanics. The Unity client keeps one connection.
///
/// Client → server: <see cref="JoinRoomGroup"/> / <see cref="LeaveRoomGroup"/> subscribe the
/// connection to a room's broadcast group and update presence.
///
/// Server → client events (emitted via <see cref="IRealtimeNotifier"/>):
///   "RoomUpdated"  (RoomStateDto)            – to the room group
///   "PlotsUpdated" (roomId, FarmPlotDto[])   – to the room group
///   "TradeUpdated" (TradeStateDto)           – to the two participants
///   "TradeInvited" (TradeStateDto)           – to the invited responder
/// </summary>
[Authorize]
public sealed class GameHub : Hub
{
    public const string RoomGroup = "room:";

    /// <summary>connectionId → roomId, so presence can be cleared on disconnect.</summary>
    private static readonly ConcurrentDictionary<string, string> ConnectionRoom = new();

    private readonly IRoomService _rooms;

    public GameHub(IRoomService rooms) => _rooms = rooms;

    public async Task JoinRoomGroup(string roomId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roomId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup + roomId);
        ConnectionRoom[Context.ConnectionId] = roomId;
        await _rooms.SetConnectedAsync(userId, roomId, true);
    }

    public async Task LeaveRoomGroup(string roomId)
    {
        var userId = Context.UserIdentifier;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup + roomId);
        ConnectionRoom.TryRemove(Context.ConnectionId, out _);
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(roomId))
            await _rooms.SetConnectedAsync(userId, roomId, false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (ConnectionRoom.TryRemove(Context.ConnectionId, out var roomId)
            && !string.IsNullOrEmpty(userId))
        {
            await _rooms.SetConnectedAsync(userId, roomId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
