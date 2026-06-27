using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChopChop.Api.Endpoints;

public static class LeaderboardEndpoints
{
    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leaderboard").WithTags("Leaderboard");

        group.MapGet("/", GetTop);
        group.MapPost("/", SubmitScore).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetTop(
        AppDbContext db, [FromQuery] string category = "default", [FromQuery] int top = 20)
    {
        top = Math.Clamp(top, 1, 100);

        var entries = await db.ScoreEntries
            .Where(s => s.Category == category)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.CreatedUtc)
            .Take(top)
            .ToListAsync();

        var dtos = entries.Select((s, i) => new ScoreEntryDto(i + 1, s.DisplayName, s.Score, s.CreatedUtc));
        return Results.Ok(dtos);
    }

    private static async Task<IResult> SubmitScore(
        [FromBody] SubmitScoreRequest request, ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub")
                     ?? throw new InvalidOperationException("Authenticated principal has no user id.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Results.Unauthorized();

        var entry = new ScoreEntry
        {
            UserId = userId,
            DisplayName = user.DisplayName,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "default" : request.Category,
            Score = request.Score,
        };
        db.ScoreEntries.Add(entry);
        await db.SaveChangesAsync();

        return Results.Created($"/api/leaderboard?category={entry.Category}", new { entry.Id });
    }
}
