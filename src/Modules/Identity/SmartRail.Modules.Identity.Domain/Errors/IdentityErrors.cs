using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Domain.Errors;

// Error codes are "Identity.<PascalCaseReason>".
public static class IdentityErrors
{
    public static readonly Error AccountExists =
        Error.Conflict("Identity.AccountExists", "An account with this email already exists. Try logging in.");

    public static readonly Error AccountNotFound =
        Error.NotFound("Identity.AccountNotFound", "This account was not found.");

    public static readonly Error DirectoryUnavailable =
        Error.Failure("Identity.DirectoryUnavailable", "We could not reach the account service. Please try again in a moment.");

    public static readonly Error TermsNotAccepted =
        Error.Validation("Identity.TermsNotAccepted", "You need to agree to the terms and the privacy notice.");
}
