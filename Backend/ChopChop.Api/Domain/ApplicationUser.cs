using Microsoft.AspNetCore.Identity;

namespace ChopChop.Api.Domain;

/// <summary>
/// The identity user for ChopChop. Extends the ASP.NET Identity user with
/// game-facing fields and a 1:1 link to the player's gameplay profile.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    public PlayerProfile? Profile { get; set; }
}
