namespace ChopChop.Api.Contracts;

public record RegisterRequest(string Email, string DisplayName, string Password);

public record LoginRequest(string Email, string Password);

public record UserDto(string Id, string Email, string DisplayName);

public record AuthResponse(string AccessToken, DateTime ExpiresUtc, UserDto User);
