using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChopChop.Api.Domain;

/// <summary>
/// Persistent gameplay state for a player. One row per <see cref="ApplicationUser"/>.
/// The <see cref="SaveDataJson"/> blob is an opaque cloud-save payload owned by the client.
/// </summary>
public class PlayerProfile
{
    [Key]
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public int Level { get; set; } = 1;

    public long Experience { get; set; }

    public int Coins { get; set; }

    public long TotalPlayTimeSeconds { get; set; }

    /// <summary>Opaque client-owned cloud-save blob (JSON). Null until first upload.</summary>
    public string? SaveDataJson { get; set; }

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
