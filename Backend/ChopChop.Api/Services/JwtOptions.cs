namespace ChopChop.Api.Services;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ChopChop.Api";
    public string Audience { get; set; } = "ChopChop.Client";
    public int AccessTokenMinutes { get; set; } = 120;
}
