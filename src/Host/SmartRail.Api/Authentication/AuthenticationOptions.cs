namespace SmartRail.Api.Authentication;

// Section "Authentication". Nothing here is a secret: the API only validates tokens.
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    // Issuer the token must carry (the address Keycloak was reached at when the token was issued).
    public string? Authority { get; init; }

    // Where signing keys are fetched from when that differs from the issuer (container networks).
    public string? MetadataAddress { get; init; }

    public string Audience { get; init; } = "smartrail-api";

    public bool RequireHttpsMetadata { get; init; } = true;

    public int ClockSkewSeconds { get; init; } = 30;
}
