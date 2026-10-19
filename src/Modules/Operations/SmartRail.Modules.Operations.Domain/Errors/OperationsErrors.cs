using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Operations.Domain.Errors;

// Error codes are "Operations.<PascalCaseReason>".
public static class OperationsErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Operations.DisruptionNotFound", $"Disruption '{id}' was not found.");

    public static readonly Error ScopeRequired =
        Error.Validation("Operations.ScopeRequired", "Name at least one station, or mark the disruption as network-wide.");

    public static readonly Error TextInvalid =
        Error.Validation("Operations.TextInvalid", "A title (up to 120 characters) and a message (up to 1000 characters) are needed, as plain text.");

    public static readonly Error ReviewByRequired =
        Error.Validation("Operations.ReviewByRequired", "A review-by time in the future is needed.");

    public static readonly Error SecondApproverRequired =
        Error.Forbidden("Operations.SecondApproverRequired", "A network-wide disruption must be approved by a different person.");

    public static readonly Error NotDraft =
        Error.Conflict("Operations.NotDraft", "Only a draft can be approved.");

    public static readonly Error AlreadyClosed =
        Error.Conflict("Operations.AlreadyClosed", "This disruption is already resolved or retracted.");
}
