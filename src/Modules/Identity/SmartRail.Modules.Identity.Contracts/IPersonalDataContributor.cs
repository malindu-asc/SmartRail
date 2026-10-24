namespace SmartRail.Modules.Identity.Contracts;

// A module that keeps personal data about a passenger (tickets, follows on the server, notifications) implements this, so the
// account can be exported and deleted in one place (privacy). The user id is the Keycloak subject.
public interface IPersonalDataContributor
{
    // A short, stable name for the export, for example "tickets".
    string Name { get; }

    // Everything the module holds about the person, as something that serialises to JSON.
    Task<object?> ExportAsync(string userId, CancellationToken cancellationToken);

    // Removes or anonymises it. Records the law requires us to keep are kept.
    Task EraseAsync(string userId, CancellationToken cancellationToken);
}
