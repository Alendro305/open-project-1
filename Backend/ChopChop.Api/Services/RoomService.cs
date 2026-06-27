using ChopChop.Api.Contracts;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using ChopChop.Api.Realtime;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Services;

public interface IRoomService
{
    Task<List<RoomSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<RoomStateDto> CreateAsync(string userId, string displayName, string name, int capacity, CancellationToken ct = default);
    Task<(bool ok, string error, RoomStateDto? state)> JoinAsync(string userId, string displayName, string roomId, CancellationToken ct = default);
    Task LeaveAsync(string userId, string roomId, CancellationToken ct = default);
    Task<RoomStateDto?> GetStateAsync(string roomId, CancellationToken ct = default);
    Task SetConnectedAsync(string userId, string roomId, bool connected, CancellationToken ct = default);
}

public sealed class RoomService : IRoomService
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotifier _notifier;

    public RoomService(AppDbContext db, IRealtimeNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<List<RoomSummaryDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Rooms
            .Where(r => r.IsActive)
            .Select(r => new RoomSummaryDto(
                r.Id, r.Name, r.HostUserId,
                r.Members.Count(m => m.LeftUtc == null),
                r.Capacity))
            .ToListAsync(ct);
    }

    public async Task<RoomStateDto> CreateAsync(string userId, string displayName, string name, int capacity, CancellationToken ct)
    {
        var room = new Room
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Room" : name.Trim(),
            HostUserId = userId,
            Capacity = Math.Clamp(capacity, 2, 32),
        };
        room.Members.Add(new RoomMember { UserId = userId, DisplayName = displayName, IsConnected = true });
        _db.Rooms.Add(room);
        Audit.Log(_db, userId, "room.create", room.Id, new { room.Name, room.Capacity });
        await _db.SaveChangesAsync(ct);

        return await GetStateAsync(room.Id, ct) ?? throw new InvalidOperationException();
    }

    public async Task<(bool ok, string error, RoomStateDto? state)> JoinAsync(string userId, string displayName, string roomId, CancellationToken ct)
    {
        var room = await _db.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive, ct);
        if (room is null) return (false, "Room not found.", null);

        var active = room.Members.Where(m => m.LeftUtc == null).ToList();
        var existing = active.FirstOrDefault(m => m.UserId == userId);
        if (existing is null)
        {
            if (active.Count >= room.Capacity) return (false, "Room is full.", null);
            room.Members.Add(new RoomMember { UserId = userId, DisplayName = displayName, IsConnected = true });
            Audit.Log(_db, userId, "room.join", roomId, new { displayName });
            await _db.SaveChangesAsync(ct);
        }

        var state = await GetStateAsync(roomId, ct);
        if (state is not null) await _notifier.RoomUpdated(state);
        return (true, string.Empty, state);
    }

    public async Task LeaveAsync(string userId, string roomId, CancellationToken ct)
    {
        var room = await _db.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == roomId, ct);
        if (room is null) return;

        foreach (var m in room.Members.Where(m => m.UserId == userId && m.LeftUtc == null))
        {
            m.LeftUtc = DateTime.UtcNow;
            m.IsConnected = false;
        }
        Audit.Log(_db, userId, "room.leave", roomId, new { });

        // Close the room when the last member leaves; reassign host if the host left.
        var remaining = room.Members.Where(m => m.LeftUtc == null).ToList();
        if (remaining.Count == 0) room.IsActive = false;
        else if (room.HostUserId == userId) room.HostUserId = remaining[0].UserId;

        await _db.SaveChangesAsync(ct);

        var state = await GetStateAsync(roomId, ct);
        if (state is not null) await _notifier.RoomUpdated(state);
    }

    public async Task<RoomStateDto?> GetStateAsync(string roomId, CancellationToken ct)
    {
        var room = await _db.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == roomId, ct);
        if (room is null) return null;

        var members = room.Members
            .Where(m => m.LeftUtc == null)
            .Select(m => new RoomMemberDto(m.UserId, m.DisplayName, m.IsConnected))
            .ToList();

        return new RoomStateDto(room.Id, room.Name, room.HostUserId, room.Capacity, members);
    }

    public async Task SetConnectedAsync(string userId, string roomId, bool connected, CancellationToken ct)
    {
        var member = await _db.RoomMembers
            .Where(m => m.RoomId == roomId && m.UserId == userId && m.LeftUtc == null)
            .OrderByDescending(m => m.JoinedUtc)
            .FirstOrDefaultAsync(ct);
        if (member is null || member.IsConnected == connected) return;

        member.IsConnected = connected;
        await _db.SaveChangesAsync(ct);

        var state = await GetStateAsync(roomId, ct);
        if (state is not null) await _notifier.RoomUpdated(state);
    }
}
