using System.ComponentModel.DataAnnotations;

namespace ChopChop.Api.Domain;

/// <summary>A single submitted leaderboard score.</summary>
public class ScoreEntry
{
    [Key]
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional category, e.g. a recipe id or game mode.</summary>
    public string Category { get; set; } = "default";

    public long Score { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
