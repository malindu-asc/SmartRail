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

    public static readonly Error PeriodInvalid =
        Error.Validation("Operations.PeriodInvalid", "The expected end must be after the start.");

    public static readonly Error SecondApproverRequired =
        Error.Forbidden("Operations.SecondApproverRequired", "A network-wide disruption must be approved by a different person.");

    public static readonly Error NotDraft =
        Error.Conflict("Operations.NotDraft", "Only a draft or a rejected notice can be sent.");

    public static readonly Error NotAwaitingApproval =
        Error.Conflict("Operations.NotAwaitingApproval", "This notice is not waiting for approval.");

    public static readonly Error CommentRequired =
        Error.Validation("Operations.CommentRequired", "A comment is required when you reject a notice.");

    public static readonly Error NotEditable =
        Error.Conflict("Operations.NotEditable", "A notice waiting for approval, or a live network-wide notice, cannot be edited. Retract it and write a new one.");

    public static readonly Error AlreadyClosed =
        Error.Conflict("Operations.AlreadyClosed", "This disruption is already resolved or retracted.");

    // ---- incidents ----

    public static Error IncidentNotFound(Guid id) => Error.NotFound("Operations.IncidentNotFound", $"Incident '{id}' was not found.");

    public static readonly Error IncidentClosed =
        Error.Conflict("Operations.IncidentClosed", "This incident is resolved.");

    public static readonly Error IncidentTitleInvalid =
        Error.Validation("Operations.IncidentTitleInvalid", "An incident needs a title of up to 120 characters.");

    public static readonly Error ConfirmationRequired =
        Error.Validation("Operations.ConfirmationRequired", "Confirm this action: it is serious and is recorded under your name.");

    public static readonly Error ReasonRequired =
        Error.Validation("Operations.ReasonRequired", "A reason is required.");

    public static readonly Error NoteRequired =
        Error.Validation("Operations.NoteRequired", "A note is required.");

    public static readonly Error SameOwner =
        Error.Validation("Operations.SameOwner", "Choose someone other than the current owner.");

    public static readonly Error NotAnOwnerCandidate =
        Error.Validation("Operations.NotAnOwnerCandidate", "That person was not found.");

    // ---- driver messages and assistance ----

    public static Error MessageNotFound(Guid id) => Error.NotFound("Operations.MessageNotFound", $"Message '{id}' was not found.");

    public static readonly Error MessageTextInvalid =
        Error.Validation("Operations.MessageTextInvalid", "A message is needed, up to 500 characters of plain text.");

    public static readonly Error RecipientRequired =
        Error.Validation("Operations.RecipientRequired", "Choose a driver, or a journey that has a driver.");

    public static readonly Error ValidUntilInvalid =
        Error.Validation("Operations.ValidUntilInvalid", "The message must stay valid until a time in the future.");

    public static readonly Error MessageExpired =
        Error.Conflict("Operations.MessageExpired", "This message is no longer valid.");

    public static Error AssistanceNotFound(Guid id) => Error.NotFound("Operations.AssistanceNotFound", $"Assistance request '{id}' was not found.");

    public static readonly Error AssistanceClosed =
        Error.Conflict("Operations.AssistanceClosed", "This request is already closed.");
}
