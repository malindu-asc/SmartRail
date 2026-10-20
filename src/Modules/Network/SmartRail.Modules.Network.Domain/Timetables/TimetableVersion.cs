using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Domain.Timetables;

// A versioned timetable. Draft and InReview can change; Published and Superseded never do (REQ-OPS-003).
public sealed class TimetableVersion : AuditableEntity
{
    private readonly List<Service> _services = [];

    private TimetableVersion() { }

    private TimetableVersion(Guid id) : base(id) { }

    public string Label { get; private set; } = string.Empty;
    public TimetableVersionStatus Status { get; private set; }
    public DateOnly? EffectiveFrom { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public IReadOnlyList<Service> Services => _services;

    public static Result<TimetableVersion> CreateDraft(string label, string author, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Length > 100 || string.IsNullOrWhiteSpace(author))
            return NetworkErrors.InvalidName;

        return new TimetableVersion(id ?? Guid.NewGuid())
        {
            Label = label.Trim(),
            Status = TimetableVersionStatus.Draft,
            Author = author.Trim()
        };
    }

    public Result AddService(Service service)
    {
        if (Status is not (TimetableVersionStatus.Draft or TimetableVersionStatus.InReview))
            return NetworkErrors.VersionNotEditable(Status.ToString());

        if (service.TimetableVersionId != Id)
            return NetworkErrors.VersionNotEditable("different");

        _services.Add(service);
        return Result.Success();
    }

    public Result RemoveService(Guid serviceId)
    {
        if (Status is not (TimetableVersionStatus.Draft or TimetableVersionStatus.InReview))
            return NetworkErrors.VersionNotEditable(Status.ToString());

        var service = _services.FirstOrDefault(s => s.Id == serviceId);
        if (service is null)
            return NetworkErrors.ServiceNotInVersion(serviceId);

        _services.Remove(service);
        return Result.Success();
    }

    public Result ChangeServiceTimes(Guid serviceId, IReadOnlyList<ServiceStopTime> times)
    {
        if (Status is not (TimetableVersionStatus.Draft or TimetableVersionStatus.InReview))
            return NetworkErrors.VersionNotEditable(Status.ToString());

        var service = _services.FirstOrDefault(s => s.Id == serviceId);
        return service is null ? NetworkErrors.ServiceNotInVersion(serviceId) : service.ChangeTimes(times);
    }

    public Result SubmitForReview()
    {
        if (Status != TimetableVersionStatus.Draft)
            return NetworkErrors.VersionWrongState(nameof(TimetableVersionStatus.Draft), Status.ToString());

        if (_services.Count == 0)
            return NetworkErrors.VersionHasNoServices;

        Status = TimetableVersionStatus.InReview;
        return Result.Success();
    }

    // The approver is a different person from the author (checked on the user, not the role).
    public Result Publish(DateOnly effectiveFrom, string approver, DateTimeOffset publishedAt)
    {
        if (Status != TimetableVersionStatus.InReview)
            return NetworkErrors.VersionWrongState(nameof(TimetableVersionStatus.InReview), Status.ToString());

        if (string.IsNullOrWhiteSpace(approver) || string.Equals(approver.Trim(), Author, StringComparison.OrdinalIgnoreCase))
            return NetworkErrors.AuthorCannotApprove;

        Status = TimetableVersionStatus.Published;
        EffectiveFrom = effectiveFrom;
        ApprovedBy = approver.Trim();
        PublishedAt = publishedAt;
        return Result.Success();
    }

    // A newer version took over. The old one is kept: it still answers for dates before the new effective date.
    public Result Supersede()
    {
        if (Status != TimetableVersionStatus.Published)
            return NetworkErrors.VersionWrongState(nameof(TimetableVersionStatus.Published), Status.ToString());

        Status = TimetableVersionStatus.Superseded;
        return Result.Success();
    }
}
