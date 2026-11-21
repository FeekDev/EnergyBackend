namespace EnergyBackend.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Key { get; init; }
    public string Issuer { get; init; } = "EnergyBackend";
    public string Audience { get; init; } = "EnergyBackend.Client";
    public int ExpirationMinutes { get; init; } = 60;
}
