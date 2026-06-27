using ChopChop.Api.Domain;

namespace ChopChop.Api.Services;

public interface ITokenService
{
    AccessToken CreateToken(ApplicationUser user, IEnumerable<string> roles);
}

public readonly record struct AccessToken(string Value, DateTime ExpiresUtc);
