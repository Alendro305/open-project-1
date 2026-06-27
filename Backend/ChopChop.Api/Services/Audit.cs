using System.Text.Json;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;

namespace ChopChop.Api.Services;

/// <summary>
/// Helper for appending to the <see cref="EventLog"/> audit trail ("store all previous states").
/// Adds the row to the context; the caller is responsible for <c>SaveChangesAsync</c> (so the audit
/// entry commits atomically with the state change it describes).
/// </summary>
public static class Audit
{
    // Ignore reference cycles so an accidental EF entity (with navigation props) never crashes a write.
    private static readonly JsonSerializerOptions Options = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        MaxDepth = 16,
    };

    public static void Log(AppDbContext db, string userId, string type, string entityId, object data)
    {
        db.EventLogs.Add(new EventLog
        {
            UserId = userId ?? string.Empty,
            Type = type,
            EntityId = entityId,
            DataJson = JsonSerializer.Serialize(data, Options),
            CreatedUtc = DateTime.UtcNow,
        });
    }
}
