namespace Frms.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SigningKey { get; init; }
    public int LifetimeMinutes { get; init; } = 60;
}

public sealed class BCryptOptions
{
    public const string SectionName = "BCrypt";
    public int WorkFactor { get; init; } = 12;
}
