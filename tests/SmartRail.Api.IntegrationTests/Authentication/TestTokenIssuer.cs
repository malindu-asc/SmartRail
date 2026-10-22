using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SmartRail.Api.IntegrationTests.Authentication;

// Issues tokens the way Keycloak would, signed with a throw-away key, so the 401 and 403 cases
// are proved without a running identity provider.
public sealed class TestTokenIssuer
{
    public const string Issuer = "https://identity.test/realms/smartrail";
    public const string Audience = "smartrail-api";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test-key" };

    public SecurityKey SigningKey => _key;

    public string Issue(
        string subject = "d0d0d0d0-0000-4000-8000-000000000001",
        string userName = "test.user",
        IEnumerable<string>? roles = null,
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null,
        SecurityKey? key = null,
        IEnumerable<string>? stations = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = subject,
            ["preferred_username"] = userName,
            ["roles"] = (roles ?? []).ToArray()
        };

        if (stations is not null)
            claims["stations"] = stations.ToArray();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow.AddMinutes(-20),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(key ?? _key, SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
