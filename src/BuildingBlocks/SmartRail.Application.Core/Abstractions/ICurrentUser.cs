namespace SmartRail.Application.Core.Abstractions;

// The authenticated caller, resolved from the Keycloak access token. Use it for audit ("who").
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
