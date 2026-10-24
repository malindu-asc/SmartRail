using System.Text.RegularExpressions;
using FluentValidation;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Identity.Application.Abstractions;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Identity.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Identity.Application.Accounts;

// ---- Create profile ----

public sealed record RegisterAccountCommand(string FullName, string Email, string Password, bool AcceptedTerms) : ICommand<RegisteredAccount>;

public sealed record RegisteredAccount(string AccountId);

public sealed partial class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100).WithMessage("Enter your full name.");
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).Must(e => EmailPattern().IsMatch(e ?? string.Empty)).WithMessage("Enter a valid email address.");
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128).WithMessage("The password needs at least 8 characters.");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}

public sealed class RegisterAccountCommandHandler(IAccountDirectory directory) : ICommandHandler<RegisterAccountCommand, RegisteredAccount>
{
    public async Task<Result<RegisteredAccount>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        if (!command.AcceptedTerms)
            return IdentityErrors.TermsNotAccepted;

        var created = await directory.CreateAsync(command.FullName.Trim(), command.Email.Trim().ToLowerInvariant(), command.Password, cancellationToken);
        return created.IsSuccess ? new RegisteredAccount(created.Value) : created.Error;
    }
}

// ---- Forgot password ----

public sealed record ForgotPasswordCommand(string Email) : ICommand;

public sealed class ForgotPasswordCommandHandler(IAccountDirectory directory) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // The answer is the same whether or not the email has an account, so nobody can find out who is registered.
        await directory.SendPasswordResetAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        return Result.Success();
    }
}

// ---- Me ----

public sealed record GetMyProfileQuery(string AccountId) : IQuery<MyProfile>;

public sealed record MyProfile(string Id, string FullName, string Email);

public sealed class GetMyProfileQueryHandler(IAccountDirectory directory) : IQueryHandler<GetMyProfileQuery, MyProfile>
{
    public async Task<Result<MyProfile>> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await directory.FindAsync(query.AccountId, cancellationToken);
        return profile is null ? IdentityErrors.AccountNotFound : new MyProfile(profile.Id, profile.FullName, profile.Email);
    }
}

// ---- Download my data ----

public sealed record ExportMyDataQuery(string AccountId) : IQuery<MyDataExport>;

public sealed record MyDataExport(DateTimeOffset ExportedAtUtc, MyProfile Profile, IReadOnlyDictionary<string, object?> Data);

public sealed class ExportMyDataQueryHandler(
    IAccountDirectory directory,
    IEnumerable<IPersonalDataContributor> contributors,
    TimeProvider clock)
    : IQueryHandler<ExportMyDataQuery, MyDataExport>
{
    public async Task<Result<MyDataExport>> Handle(ExportMyDataQuery query, CancellationToken cancellationToken)
    {
        var profile = await directory.FindAsync(query.AccountId, cancellationToken);
        if (profile is null)
            return IdentityErrors.AccountNotFound;

        var data = new Dictionary<string, object?>();
        foreach (var contributor in contributors)
            data[contributor.Name] = await contributor.ExportAsync(query.AccountId, cancellationToken);

        return new MyDataExport(clock.GetUtcNow(), new MyProfile(profile.Id, profile.FullName, profile.Email), data);
    }
}

// ---- Delete my data ----

public sealed record DeleteMyAccountCommand(string AccountId) : ICommand;

public sealed class DeleteMyAccountCommandHandler(IAccountDirectory directory, IEnumerable<IPersonalDataContributor> contributors)
    : ICommandHandler<DeleteMyAccountCommand>
{
    public async Task<Result> Handle(DeleteMyAccountCommand command, CancellationToken cancellationToken)
    {
        // Modules first, the account last: if a module fails the person can still sign in and try again.
        foreach (var contributor in contributors)
            await contributor.EraseAsync(command.AccountId, cancellationToken);

        return await directory.DeleteAsync(command.AccountId, cancellationToken);
    }
}
