using System.Security.Claims;

namespace ChopChop.Api.Services;

public static class ClaimsExtensions
{
    /// <summary>The authenticated user's id (JWT <c>sub</c> / NameIdentifier), or null.</summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

    public static string GetUserIdOrThrow(this ClaimsPrincipal principal)
        => principal.GetUserId() ?? throw new InvalidOperationException("Authenticated principal has no user id.");

    public static string GetDisplayName(this ClaimsPrincipal principal)
        => principal.FindFirstValue("displayName") ?? principal.FindFirstValue(ClaimTypes.Name) ?? "Player";
}
