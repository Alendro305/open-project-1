using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

        group.MapGet("/", GetProfile);
        group.MapPut("/", UpdateProfile);
        group.MapGet("/save", GetSave);
        group.MapPut("/save", UploadSave);

        return app;
    }

    private static async Task<IResult> GetProfile(ClaimsPrincipal principal, AppDbContext db)
    {
        var profile = await LoadOrCreateProfile(principal, db);
        return Results.Ok(ToDto(profile));
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request, ClaimsPrincipal principal, AppDbContext db)
    {
        var profile = await LoadOrCreateProfile(principal, db);

        if (request.DisplayName is not null)
        {
            var user = await db.Users.FirstAsync(u => u.Id == profile.UserId);
            user.DisplayName = request.DisplayName;
        }
        if (request.Level is { } level) profile.Level = level;
        if (request.Experience is { } xp) profile.Experience = xp;
        if (request.Coins is { } coins) profile.Coins = coins;
        if (request.TotalPlayTimeSeconds is { } seconds) profile.TotalPlayTimeSeconds = seconds;

        profile.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(ToDto(profile));
    }

    private static async Task<IResult> GetSave(ClaimsPrincipal principal, AppDbContext db)
    {
        var profile = await LoadOrCreateProfile(principal, db);
        return Results.Ok(new SaveDataDto(profile.SaveDataJson, profile.UpdatedUtc));
    }

    private static async Task<IResult> UploadSave(
        [FromBody] UploadSaveRequest request, ClaimsPrincipal principal, AppDbContext db)
    {
        var profile = await LoadOrCreateProfile(principal, db);
        profile.SaveDataJson = request.SaveDataJson;
        profile.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new SaveDataDto(profile.SaveDataJson, profile.UpdatedUtc));
    }

    private static async Task<PlayerProfile> LoadOrCreateProfile(ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub")
                     ?? throw new InvalidOperationException("Authenticated principal has no user id.");

        var profile = await db.PlayerProfiles.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            profile = new PlayerProfile { UserId = userId };
            db.PlayerProfiles.Add(profile);
            await db.SaveChangesAsync();
        }
        return profile;
    }

    private static ProfileDto ToDto(PlayerProfile p) => new(
        p.UserId, p.User?.DisplayName ?? string.Empty, p.Level, p.Experience,
        p.Coins, p.TotalPlayTimeSeconds, p.UpdatedUtc);
}
