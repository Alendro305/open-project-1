using System.Security.Claims;
using ChopChop.Api.Contracts;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using ChopChop.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChopChop.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        UserManager<ApplicationUser> users,
        AppDbContext db,
        ITokenService tokens)
    {
        var existing = await users.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Results.Conflict(new { error = "An account with that email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Email : request.DisplayName,
            Profile = new PlayerProfile(),
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Results.BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Results.Ok(await BuildAuthResponse(user, users, tokens));
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        UserManager<ApplicationUser> users,
        ITokenService tokens)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Results.Json(new { error = "Invalid email or password." }, statusCode: StatusCodes.Status401Unauthorized);

        user.LastSeenUtc = DateTime.UtcNow;
        await users.UpdateAsync(user);

        return Results.Ok(await BuildAuthResponse(user, users, tokens));
    }

    private static async Task<IResult> Me(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> users)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();
        return Results.Ok(new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName));
    }

    private static async Task<AuthResponse> BuildAuthResponse(
        ApplicationUser user, UserManager<ApplicationUser> users, ITokenService tokens)
    {
        var roles = await users.GetRolesAsync(user);
        var token = tokens.CreateToken(user, roles);
        var dto = new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName);
        return new AuthResponse(token.Value, token.ExpiresUtc, dto);
    }
}
