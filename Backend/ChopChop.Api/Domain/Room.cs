using System.ComponentModel.DataAnnotations;

namespace ChopChop.Api.Domain;

/// <summary>A logical lobby/session players join. Membership + presence; no transform replication.</summary>
public class Room
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string HostUserId { get; set; } = string.Empty;

    public int Capacity { get; set; } = 8;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<RoomMember> Members { get; set; } = new();
    public List<FarmPlot> Plots { get; set; } = new();
}

/// <summary>Membership of a <see cref="Room"/>. A row persists join history; <see cref="LeftUtc"/>
/// is set on leave and <see cref="IsConnected"/> tracks live SignalR presence.</summary>
public class RoomMember
{
    [Key]
    public int Id { get; set; }

    public string RoomId { get; set; } = string.Empty;
    public Room? Room { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public bool IsConnected { get; set; }

    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LeftUtc { get; set; }
}
