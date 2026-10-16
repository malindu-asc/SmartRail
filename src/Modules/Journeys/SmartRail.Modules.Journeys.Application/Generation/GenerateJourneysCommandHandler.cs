using Microsoft.Extensions.Logging;
using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Journeys.Application.Abstractions;
using SmartRail.Modules.Journeys.Domain.Journeys;
using SmartRail.Modules.Network.Contracts;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Journeys.Application.Generation;

public sealed class GenerateJourneysCommandHandler(
    INetworkTimetable timetable,
    INetworkTimeZone timeZone,
    IJourneyRepository journeys,
    IJourneysUnitOfWork unitOfWork,
    ILogger<GenerateJourneysCommandHandler> logger)
    : ICommandHandler<GenerateJourneysCommand, GenerationResult>
{
    public async Task<Result<GenerationResult>> Handle(GenerateJourneysCommand command, CancellationToken cancellationToken)
    {
        var created = 0;
        var skipped = 0;
        var failed = 0;
        var withoutServices = new List<DateOnly>();

        for (var offset = 0; offset < command.Days; offset++)
        {
            var date = command.FromDate.AddDays(offset);
            var services = await timetable.GetServicesRunningOnAsync(date, cancellationToken);
            if (services.Count == 0)
            {
                withoutServices.Add(date);
                continue;
            }

            // Existing journeys are never touched: a new timetable version only reaches dates with no journeys yet.
            var existing = await journeys.GetGeneratedServiceIdsAsync(date, cancellationToken);

            var addedForDate = 0;
            foreach (var service in services)
            {
                if (existing.Contains(service.ServiceId))
                {
                    skipped++;
                    continue;
                }

                var journey = Build(service, date, timeZone);
                if (journey.IsFailure)
                {
                    failed++;
                    logger.LogWarning("Journey for service {Service} on {Date} could not be built: {Error}",
                        service.TrainNumber, date, journey.Error.Code);
                    continue;
                }

                journeys.Add(journey.Value);
                addedForDate++;
            }

            if (addedForDate > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);   // one transaction per date

            created += addedForDate;
            logger.LogInformation("Journey generation for {Date}: {Created} created, {Skipped} already existed",
                date, addedForDate, services.Count - addedForDate - failed);
        }

        return new GenerationResult(created, skipped, failed, withoutServices);
    }

    // Scheduled times are service-date midnight in the network time zone plus the stop's minutes, converted to UTC once.
    private static Result<Journey> Build(ScheduledService service, DateOnly date, INetworkTimeZone timeZone)
    {
        var drafts = service.Stops.Select(stop =>
        {
            var arrival = stop.ArrivalMinutes is { } a ? timeZone.ToUtc(date, a) : (DateTimeOffset?)null;
            var departure = stop.DepartureMinutes is { } d ? timeZone.ToUtc(date, d) : (DateTimeOffset?)null;
            var dwell = stop.ArrivalMinutes is { } from && stop.DepartureMinutes is { } to ? to - from : 0;

            return new JourneyStopDraft(
                stop.Sequence,
                stop.StationCode,
                Enum.Parse<JourneyStopKind>(stop.Kind),
                arrival,
                departure,
                dwell,
                stop.PlatformCode);
        }).ToList();

        return Journey.Create(
            service.ServiceId,
            service.TimetableVersionId,
            date,
            service.TrainNumber,
            service.RouteCode,
            JourneyCreationOrigin.Generated,
            null,
            drafts);
    }
}
