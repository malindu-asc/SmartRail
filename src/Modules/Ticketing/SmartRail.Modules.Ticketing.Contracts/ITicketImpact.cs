namespace SmartRail.Modules.Ticketing.Contracts;

// How many tickets sit on a journey, for the controller's "who will be affected" preview before a cancellation (UC-OPS-02).
public interface ITicketImpact
{
    Task<TicketImpactCount> CountAsync(Guid journeyId, CancellationToken cancellationToken);
}

public sealed record TicketImpactCount(int Tickets, int Travellers);
