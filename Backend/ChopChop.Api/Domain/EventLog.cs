using System.ComponentModel.DataAnnotations;

namespace ChopChop.Api.Domain;

/// <summary>
/// Append-only audit trail. Every meaningful state transition (room join/leave, trade offer/confirm/
/// complete, plot plant/water/harvest, inventory change) is written here so previous states are
/// durably persisted and the full history can be reconstructed. Current state lives in the domain
/// tables; this is the "all previous states" record.
/// </summary>
public class EventLog
{
    [Key]
    public long Id { get; set; }

    /// <summary>Actor who caused the event (may be empty for system events).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Dotted event type, e.g. "room.join", "trade.confirm", "plot.harvest".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Id of the entity the event concerns (room id, trade id, plot id…).</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the relevant state at the time of the event.</summary>
    public string DataJson { get; set; } = "{}";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
