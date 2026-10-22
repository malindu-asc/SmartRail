using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Application.Abstractions;

// Where passenger accounts live (Keycloak). The API never stores passwords.
public interface IAccountDirectory
{
    // Creates a passenger and returns its id. AccountExists when the email is taken.
    Task<Result<string>> CreateAsync(string fullName, string email, string password, CancellationToken cancellationToken);

    Task<AccountProfile?> FindAsync(string accountId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(string accountId, CancellationToken cancellationToken);

    // Sends the reset link. Never says whether the email has an account.
    Task SendPasswordResetAsync(string email, CancellationToken cancellationToken);
}

public sealed record AccountProfile(string Id, string FullName, string Email);
